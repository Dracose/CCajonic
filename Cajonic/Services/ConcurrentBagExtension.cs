using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace Cajonic.Services
{
    public static class ConcurrentBagExtension
    {
        public static void AddRange<T>(this ConcurrentBag<T> bag, IEnumerable<T> toAdd)
        {
            foreach (var element in toAdd)
            {
                bag.Add(element);
            }
        }
    }
}
