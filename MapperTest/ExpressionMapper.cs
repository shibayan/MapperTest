using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace MapperTest
{
    class ExpressionMapper
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
            var srcType = typeof(TSrc);
            var dstType = typeof(TDst);

            var key = string.Format("{0}_{1}_{2}", srcType.FullName, dstType.FullName, string.Join(",", excludeProperties));
            var mapping = _mappingCache.ContainsKey(key) ? _mappingCache[key] as Action<TSrc, TDst> : null;
            if (mapping == null)
            {
                var srcParam = Expression.Parameter(typeof(TSrc), "src");
                var dstParam = Expression.Parameter(typeof(TDst), "dst");

                var assigns = new List<BinaryExpression>();
                foreach (var dstProp in dst.GetType().GetProperties().Where(p =>
                    p.CanWrite &&
                    (p.PropertyType == typeof(string) || p.PropertyType.IsValueType) &&
                    !excludeProperties.Contains(p.Name)))
                {
                    var srcProp = srcType.GetProperty(dstProp.Name);
                    if (srcProp != null && srcProp.CanRead)
                        assigns.Add(Expression.Assign(Expression.Property(dstParam, dstProp.Name), Expression.Property(srcParam, dstProp.Name)));
                }

                var lamda = Expression.Lambda<Action<TSrc, TDst>>(Expression.Block(assigns), srcParam, dstParam);
                mapping = lamda.Compile() as Action<TSrc, TDst>;
                _mappingCache[key] = mapping;
            }

            mapping(src, dst);
        }

        private static readonly Dictionary<string, object> _mappingCache = new Dictionary<string, object>();

        
    }
}
