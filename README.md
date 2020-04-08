## Validation Monad 

Реализациия Монады валидации*, для упрощения работы с ошибками доменного уровня.

## Мотивация

Исключительные ситуации, возникающие в программах, можно условно разделить на 2 основные группы:
 - Исключения, приводящие программу в неопределнное состояние (aka panics). Обычно тактие ошибки не обрабатываются на месте, а перехватываются и логгируются на самом верхнем уровне. Примеры таких ошибок - NullReference, OutOfMemory, и проч.
 - Ошибки, являющиеся частью доменной модели. Такие ошибки являются частью бизнесс-процесса и должны быть учтены при его проектировании. Примером таких ошибок могут служить ошибки валидации введенной пользователем формы, отправка строки некорректного формата серверу и проч.
    
В языке C# стандартным и единственным подходом обработки ошибок в программе является использование исключений. Этот подход отлично подходит для работы с ошибками из первой группы, но для второй его использование не так удобно. Во первых, исключения ломают обычный процесс исполнения программы. Во вторых, по сигнатуре функции невозможно понять, выбрасывает ли она исключение (с этим в определенной степени справляются комментарии, но иногда после рафакторинга про них все забывают). В третьих, обработка исключений очень многословна и способна существенно раздуть размер кода. 

В функциональных языках для обработки подобного рода ошибок обчно используется тип-сумма. Объект такого типа может хранить либо значение, либо найденную ошибку (например [Either](http://hackage.haskell.org/package/base-4.12.0.0/docs/Data-Either.html) в Haskell, [Result](https://docs.microsoft.com/en-us/dotnet/fsharp/language-reference/results) в F#). Преимущество этого подхода заключается в первую очередь в нагладности - по типу возращаемого значения функции сразу можно понять, что она можно вернуть ошибку. Во вторых, использование указанных типов-оберток не позволит просто так достать из них значение, опустив при этом обработку исключительных ситуаций. 

В отличие от типов вроде Either, которые обычно содержат только первую найденную ошибку, представленный в библиотеке тип Validation способен их аккумулировать. Это удобно использовать для задачи валидации, когда хочется сразу вернуть пользователю список найденных ошибок, а не ограничиваться только одной. 

К сожалению, многие абстракции ФП невозможно применить на C# ввиду особенностей синтаксиса, однако основные из них - Монаду и Функтор можно реализовать средствами LINQ.

# Пример использования

Пусть стоит задача реализовать валидацию простой формы, включающей в себя логин пользователя и его email. Напишем пару методов для валидации отдельных частей формы:

```cs
public IValidation<string> ValidateUsername(string username)
{
    if (string.IsNullOrEmpty(username))
    {
        return Validation.Failure<string>("Username is empty");
    }
    if (!username.Contains("test"))
    {
        return Validation.Failure<string>("No such user");
    }
    return Validation.Success(username);
}

public IValidation<string> ValidateEmail(string email)
{
    if (string.IsNullOrWhiteSpace(email))
    {
        return Validation.Failure<string>("Email is empty");
    }
    
    if (!email.Contains("@"))
    {
        return Validation.Failure<string>("Email must contain @-sign"); 
    }       
        
    return Validation.Success(email);
}
```

Создадим также класс формы:

```cs
public class FormData
{
    public string Username { get; set; }
    public string Email { get; set; }
}
```

Далее есть несколько возможных подходов к комбинированию этих функций, от которых зависит конечный результат:

```cs
// возращает объект FormData или первую найденную ошибку
public IValidation<FormData> Validate1(string username, string email)
{   
    return from validatedUsername in ValidateUsername(username)    // typeof(validatedUsername) == string
           from validatedEmail in ValidateEmail(email)             // typeof(validatedEmail)    == string
           select new FormData { Username = validatedUsername, Email = validatedEmail };
}


// возращает объект FormData или все найденные ошибки
public IValidation<FormData> Validate2(string username, string email)
{
    return from _ in Validation.DefaultSuccess()               // заглушка для начала do-нотации
           
           // раздельно валидируем username и email а затем объединям результат валидации
           let wrappedUsername = ValidateUsername(username)    // typeof(wrappedUsername) == IValidation<string>
           let wrappedEmail = ValidateEmail(email)             // typeof(wrappedEmail)    == IValidation<string>
           let wrappedAll = wrappedUsername.ZipWith(wrappedEmail, (validatedUsername, validatedEmail) => (validatedUsername, validatedEmail))                                     
           
           // конвертирует результаты независимой валидации в FormData
           from data in wrappedAll
           select new FormData { Username = data.validatedUsername, Email = data.validatedEmail  };
}


// возращает объект FormData или все найденные ошибки
public IValidation<FormData> Validate3(string username, string email)
{
    // строим функцию создания объекта
    Func<string, string, FormData> objectBulider = (uname, mail) => new FormData { Username = uname, Email = mail };
    
    // вносим ее в контекст IValidation (Lift позволяет преобразовать функцию вида (a, b) => c в функцию вида (IValidation<a>, IValidation<b>) => IValidation<c>)
    Func<IValidation<string>, IValidation<string>, IValidation<FormData>> liftedObjectBulider = objectBulider.Lift();
    
    // валидируем 
    var validatedUsername = ValidateUsername(username);
    var validatedEmail = ValidateEmail(email);
        
    // передаем функции создания наши результаты валидации
    return liftedObjectBulider(validatedUsername, validatedEmail)
}
```
