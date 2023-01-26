using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CueFileGen
{
    public interface IResult<TSuccess, TError>
    {
        bool IsSuccess();
        bool IsError();

        Result.Ok<TSuccess, TError>? getResult();
        Result.Error<TSuccess, TError>? getError();
    }

    namespace Result
    {
        public class Ok<TSuccess, TError> : IResult<TSuccess, TError>
        {
            public Error<TSuccess, TError>? getError() => null;

            public Ok<TSuccess, TError>? getResult() => this;

            public bool IsError() => false;

            public bool IsSuccess() => true;

            public TSuccess Value { get; }

            public Ok(TSuccess value)
            {
                Value = value;
            }
        }

        public class Error<TSuccess, TError> : IResult<TSuccess, TError>
        {
            public Error<TSuccess, TError>? getError() => this;

            public Ok<TSuccess, TError>? getResult() => null;
            public bool IsError() => true;

            public bool IsSuccess() => false;

            public TError What { get; }

            public Error(TError error)
            {
                What = error;
            }
        }
    }
}
