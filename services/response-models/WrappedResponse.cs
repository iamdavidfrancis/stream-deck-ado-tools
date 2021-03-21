using System;
using System.Collections.Generic;
using System.Text;

namespace StreamDeckAzureDevOps.Services.ResponseModels
{
    public class WrappedResponse<T>
    {
        public int Count { get; set; }

        public T[] Value { get; set; }
    }
}
