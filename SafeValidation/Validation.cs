using System;
using System.Collections.Generic;
using System.Linq;

namespace SafeValidation
{
    /// <summary>
    /// Validation objects builder
    /// </summary>
    public static class Validation
    {
        /// <summary>
        /// Builds <see cref="IValidation{T}"/> object with "success" state
        /// </summary>
        /// <typeparam name="T">Type</typeparam>
        /// <param name="value">Value</param>
        /// <returns><see cref="IValidation{T}"/> object with "success" state</returns>
        public static IValidation<T> Success<T>(T value) 
            => new ValidationImpl<T> { Value = value };

        /// <summary>
        /// Builds <see cref="IValidation{T}"/> object with "failure" state
        /// </summary>
        /// <typeparam name="T">Type</typeparam>
        /// <param name="message">Error message</param>
        /// <returns><see cref="IValidation{T}"/> object with "failure" state</returns>
        public static IValidation<T> Failure<T>(string message)
            => new ValidationImpl<T> { Errors = new[] { message } };

        /// <summary>
        /// Builds <see cref="IValidation{T}"/> object with "failure" state
        /// </summary>
        /// <typeparam name="T">Type</typeparam>
        /// <param name="messages">Error messages</param>
        /// <returns><see cref="IValidation{T}"/> object with "failure" state</returns>
        public static IValidation<T> Failure<T>(IEnumerable<string> messages)
            => new ValidationImpl<T> { Errors = messages.ToArray() };

        /// <summary>
        /// Lifts result of unsafe function to <see cref="IValidation{T}"/> context
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="function">Function that can throw an exception</param>
        /// <returns>Result of function wrapped with <see cref="IValidation{T}"/></returns>
        public static IValidation<T> FromThrowable<T>(Func<T> function)
        {
            IValidation<T> result;
            try
            {
                result = Success(function());
            }
            catch (Exception e)
            {
                result = Failure<T>(e.ToString());
            }
            return result;
        }

        /// <summary>
        /// Lifts result of unsafe function to <see cref="IValidation{T}"/> context
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="function">Function that can throw an exception</param>
        /// <param name="exceptionProjection">Custom Exception => string projection</param>
        /// <returns>Result of function wrapped with <see cref="IValidation{T}"/></returns>
        public static IValidation<T> FromThrowable<T>(Func<T> function, Func<Exception, string> exceptionProjection)
        {
            IValidation<T> result;
            try
            {
                result = Success(function());
            }
            catch (Exception e)
            {
                result = Failure<T>(exceptionProjection(e));
            }
            return result;
        }
    }
}
