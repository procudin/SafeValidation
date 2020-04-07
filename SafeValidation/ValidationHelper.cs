using System;
using System.Linq;

namespace SafeValidation
{
    public static class ValidationHelper
    {
        /// <summary>
        /// Implementation of Monad's bind function (also known as chain, flatMap, (>>=)).
        /// Converts source to target using conversion from resultSelector function.
        /// Сan be used to combine results for functions like A => IValidation{B}
        /// </summary>
        /// <typeparam name="TSource">Source validation type</typeparam>
        /// <typeparam name="TResult">Target validation type</typeparam>
        /// <param name="source">Source data</param>
        /// <param name="resultSelector">Result selector</param>
        /// <returns>Result of combining</returns>
        public static IValidation<TResult> SelectMany<TSource, TResult>(
            this IValidation<TSource> source, 
            Func<TSource, IValidation<TResult>> resultSelector)
        {
            if (source.IsFailure)
            {
                return Validation.Failure<TResult>(source.Errors);
            }

            return resultSelector(source.UnsafeUnwrap());
        }

        /// <summary>
        /// Implementation of bind monad function (also known as chain, flatMap, (>>=)).
        /// Additional implementation for full LINQ support.
        /// </summary>
        /// <typeparam name="TSource">Source validation type</typeparam>
        /// <typeparam name="TIntermediate">Intermediate validation type</typeparam>
        /// <typeparam name="TResult">Target validation type</typeparam>
        /// <param name="source">Source data</param>
        /// <param name="resultSelector">Intermediate selector</param>
        /// <param name="resultSelector">Result selector</param>
        /// <returns>Result of combining</returns>
        public static IValidation<TResult> SelectMany<TSource, TIntermediate, TResult>(
            this IValidation<TSource> source,
            Func<TSource, IValidation<TIntermediate>> intermediateSelector,
            Func<TSource, TIntermediate, TResult> resultSelector)
        {
            return source.SelectMany(v =>
            {
                var res = intermediateSelector(v);
                if (res.IsFailure)
                {
                    return Validation.Failure<TResult>(res.Errors);
                }

                return Validation.Success(resultSelector(v, res.UnsafeUnwrap()));
            });
        }

        /// <summary>
        /// Implementation of Functor's map function.
        /// Converts source to target using conversion from projection function. 
        /// </summary>
        /// <typeparam name="TSource">Source validation type</typeparam>
        /// <typeparam name="TResult">Target validation type</typeparam>
        /// <param name="source">Source data</param>
        /// <param name="projection">Result selector</param>
        /// <returns>Validation of TResult type</returns>
        public static IValidation<TResult> Select<TSource, TResult>(this IValidation<TSource> source, Func<TSource, TResult> projection)
        {
            if (source.IsFailure)
            {
                return Validation.Failure<TResult>(source.Errors);
            }

            return Validation.Success<TResult>(projection(source.UnsafeUnwrap()));
        }

        /// <summary>
        /// Implementation of Applicative functor's apply function.
        /// Made just for fun, it does not make practical sense now.
        /// It may be used in the future for implementing Lift function
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="R"></typeparam>
        /// <param name="func">Function with IValidation context </param>
        /// <param name="value">Value with IValidation context</param>
        /// <returns>Result of combining</returns>
        public static IValidation<R> Apply<T, R>(this IValidation<Func<T, R>> func, IValidation<T> value)
        {
            if (func.IsFailure && value.IsFailure)
            {
                return Validation.Failure<R>(func.Errors.Concat(value.Errors));
            }

            if (func.IsFailure)
            {
                return Validation.Failure<R>(func.Errors);
            }

            if (value.IsFailure)
            {
                return Validation.Failure<R>(value.Errors);
            }

            return Validation.Success<R>(func.UnsafeUnwrap()(value.UnsafeUnwrap()));
        }

        /// <summary>
        /// Combines two <see cref="IValidation"/> with resultSelector function
        /// </summary>
        /// <typeparam name="T1">Type of first <see cref="IValidation{T}"/></typeparam>
        /// <typeparam name="T2">Type of second <see cref="IValidation{T}"/></typeparam>
        /// <typeparam name="R">Type of result <see cref="IValidation{T}"/></typeparam>
        /// <param name="first">First object</param>
        /// <param name="second">Second object</param>
        /// <param name="resultSelector">Result selector</param>
        /// <returns>Combined <see cref="IValidation{T}"/></returns>
        public static IValidation<R> ZipWith<T1, T2, R>(this IValidation<T1> first, IValidation<T2> second, Func<T1, T2, R> resultSelector)
        {
            // Can be implemented implicitly with Apply, but done exlplicitly due to perfommance issue.
            if (first.IsFailure && second.IsFailure)
            {
                return Validation.Failure<R>(first.Errors.Union(second.Errors));
            }

            if (first.IsFailure)
            {
                return Validation.Failure<R>(first.Errors);
            }

            if (second.IsFailure)
            {
                return Validation.Failure<R>(second.Errors);
            }

            return Validation.Success<R>(resultSelector(first.UnsafeUnwrap(), second.UnsafeUnwrap()));
        }

        /// <summary>
        /// Combines two <see cref="IValidation"/> into <see cref="Tuple{T1, T2}"/>
        /// </summary>
        /// <typeparam name="T1">Type of first <see cref="IValidation{T}"/></typeparam>
        /// <typeparam name="T2">Type of second <see cref="IValidation{T}"/></typeparam>
        /// <param name="first">First object</param>
        /// <param name="second">Second object</param>
        /// <returns>Combined <see cref="IValidation{T}"/></returns>
        public static IValidation<Tuple<T1, T2>> Zip<T1, T2>(this IValidation<T1> first, IValidation<T2> second)
        {
            return first.ZipWith(second, Tuple.Create);
        }

        /// <summary>
        /// Combines two objects and returns Validation of left type
        /// </summary>
        /// <typeparam name="T1">Type of first <see cref="IValidation{T}"/></typeparam>
        /// <typeparam name="T2">Type of second <see cref="IValidation{T}"/></typeparam>
        /// <param name="first">First object</param>
        /// <param name="second">Second object</param>
        /// <returns>Validation of left type</returns>
        public static IValidation<T1> ZipLeft<T1, T2>(this IValidation<T1> first, IValidation<T2> second)
        {
            return first.ZipWith(second, (fst, snd) => fst);
        }

        /// <summary>
        /// Combines two objects and returns Validation of right type
        /// </summary>
        /// <typeparam name="T1">Type of first <see cref="IValidation{T}"/></typeparam>
        /// <typeparam name="T2">Type of second <see cref="IValidation{T}"/></typeparam>
        /// <param name="first">First object</param>
        /// <param name="second">Second object</param>
        /// <returns>Validation of right type</returns>
        public static IValidation<T2> ZipRight<T1, T2>(this IValidation<T1> first, IValidation<T2> second)
        {
            return first.ZipWith(second, (fst, snd) => snd);
        }

        /// <summary>
        /// Combines three <see cref="IValidation{T}"/> with resultSelector function
        /// </summary>
        /// <typeparam name="T1">Type of first <see cref="IValidation{T}"/></typeparam>
        /// <typeparam name="T2">Type of second <see cref="IValidation{T}"/></typeparam>
        /// <typeparam name="T3">Type of third <see cref="IValidation{T}"/></typeparam>
        /// <typeparam name="R">Type of result <see cref="IValidation{T}"/></typeparam>
        /// <param name="first">First object</param>
        /// <param name="second">Second object</param>
        /// <param name="third">Second object</param>
        /// <param name="resultSelector">Result selector</param>
        /// <returns>Combined <see cref="IValidation{T}"/></returns>
        public static IValidation<R> ZipWith3<T1, T2, T3, R>(this IValidation<T1> first, IValidation<T2> second, IValidation<T3> third, Func<T1, T2, T3, R> resultSelector)
        {
            return first.Zip(second).ZipWith(third, (a1, a2) => resultSelector(a1.Item1, a1.Item2, a2));
        }

        /// <summary>
        /// Combines three <see cref="IValidation"/> into <see cref="Tuple{T1, T2, T3}"/>
        /// </summary>
        /// <typeparam name="T1">Type of first <see cref="IValidation{T}"/></typeparam>
        /// <typeparam name="T2">Type of second <see cref="IValidation{T}"/></typeparam>
        /// <typeparam name="T3">Type of third <see cref="IValidation{T}"/></typeparam>
        /// <param name="first">First object</param>
        /// <param name="second">Second object</param>
        /// <param name="third">Third object</param>
        /// <returns>Combined <see cref="IValidation{T}"/></returns>
        public static IValidation<Tuple<T1, T2, T3>> Zip3<T1, T2, T3>(this IValidation<T1> first, IValidation<T2> second, IValidation<T3> third)
        {
            return ZipWith3(first, second, third, Tuple.Create);
        }
    }
}
