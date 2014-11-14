using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapperTest
{
    // primitive type, struct, enum, nullable and string propety is copied
    public static class StandardMapper
    {
        public static TDst Map<TSrc, TDst>(TSrc input, params string[] excludeProperties)
            where TSrc : class
            where TDst : class, new()
        {
            var dst = new TDst();
            Map<TSrc, TDst>(input, dst, excludeProperties);
            return dst;
        }
        public static List<TDst> Map<TSrc, TDst>(IEnumerable<TSrc> list, params string[] excludeProperties)
            where TSrc : class
            where TDst : class, new()
        {
            var dst = new List<TDst>();
            foreach (var obj in list)
                dst.Add(Map<TSrc, TDst>(obj, excludeProperties));
            return dst;
        }
        public static void Map<TSrc, TDst>(TSrc src, TDst dst, params string[] excludeProperties)
        {
            var srcType = src.GetType();
            foreach (var dstProp in dst.GetType().GetProperties().Where(p =>
                p.CanWrite &&
                (p.PropertyType == typeof(string) || p.PropertyType.IsValueType) &&
                !excludeProperties.Contains(p.Name)))
            {
                var srcProp = srcType.GetProperty(dstProp.Name);
                if (srcProp != null && srcProp.CanRead)
                    dstProp.SetValue(dst, srcProp.GetValue(src, null), null);
            }
        }
    }
}
