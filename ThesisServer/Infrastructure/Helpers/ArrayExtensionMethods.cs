using System.Collections.Generic;

namespace ThesisServer.Infrastructure.Helpers
{
    public static class ArrayExtensionMethods
    {
        public static (TResult[] Bytes, int OrderNumber)[] Chunk<TResult>(this TResult[] array, int maxChunkSize)
        {
           // var numberOfChunksNeeded = (int)Math.Ceiling((double) array.Length / (double) maxChunkSize);

            var response = new List<(TResult[] Bytes, int OrderNumber)>();
            int orderNumber = 0;

            for (int i = 0; i < array.Length; i++)
            {
                var j = 0;
                TResult[] temp = new TResult[maxChunkSize];

                while (j < maxChunkSize && i < array.Length)
                {
                    temp[j] = array[i];
                    j++;
                    i++;
                }

                i--;

                if (j != maxChunkSize)
                {
                    var temp2 = new TResult[j];

                    for (int k = 0; k < j; k++)
                    {
                        temp2[k] = temp[k];
                    }

                    temp = temp2;
                }

                response.Add((temp, orderNumber));
                orderNumber++;
            }

            return response.ToArray();
        }
    }
}