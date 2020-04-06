namespace SafeValidation
{
    /// <summary>
    /// Represents value with two possibilities: either T or array of errors.
    /// Used to represent a value which is either correct or have errors
    /// </summary>
    public interface IValidation<T>
    {
        /// <summary>
        /// Is value have errors. Should always be equal to the inversed IsSuccess.
        /// </summary>
        bool IsFailure { get; }

        /// <summary>
        /// Is value correct. Should always be equal to the inversed IsFailure.
        /// </summary>
        bool IsSuccess { get; }

        /// <summary>
        /// Array of encountered errors
        /// </summary>
        string[] Errors { get; }

        /// <summary>
        /// Unsafe converstion to <typeparamref name="T"/>
        /// </summary>
        /// <exception cref="System.Exception">Wheter object in failure state</exception>
        /// <returns>Unwrapped value</returns>
        T UnsafeUnwrap();

        /// <summary>
        /// Safe converstion to <typeparamref name="T"/>
        /// </summary>
        /// <param name="default">Value that be used fo failure state</param>
        /// <returns>Unwrapped value</returns>
        T Unwrap(T @default);
    }
}
