using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SafeValidation
{
    /// <summary>
    /// Inner implementation of <see cref="IValidation{T}"/> interface 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class ValidationImpl<T> : IValidation<T>
    {
        public bool IsFailure => this.Errors.Length > 0;

        public bool IsSuccess => this.Errors.Length == 0;

        public string[] Errors { get; internal set; }

        internal T Value { get; set; }

        public T UnsafeUnwrap()
        {
            if (this.IsFailure)
            {
                throw new Exception("Cannot unwrap IValidation with error state");
            }
            return this.Value;
        }

        public T Unwrap(T @default)
        {
            return this.IsFailure ? @default : this.Value;
        }
    }
}
