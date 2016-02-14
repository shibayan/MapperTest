using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace MicroMapper
{
    public class Mapper
    {
        internal Mapper(Dictionary<string, object> mapperCache)
        {
            _mapperCache = mapperCache;
        }

        private readonly Dictionary<string, object> _mapperCache;

        public TDestination Map<TSource, TDestination>(TSource source) where TDestination : new()
        {
            var destination = new TDestination();

            Map(source, destination);

            return destination;
        }

        public void Map<TSource, TDestination>(TSource source, TDestination destination)
        {
            var mapper = GetMapper<TSource, TDestination>();

            mapper(source, destination);
        }

        public IList<TDestination> MapArray<TSource, TDestination>(IList<TSource> sources) where TDestination : new()
        {
            var destinations = new TDestination[sources.Count];

            MapArray(sources, destinations);

            return destinations;
        }

        public void MapArray<TSource, TDestination>(IList<TSource> sources, IList<TDestination> destinations) where TDestination : new()
        {
            var mapper = GetMapper<TSource, TDestination>();

            for (int i = 0; i < sources.Count; i++)
            {
                destinations[i] = new TDestination();

                mapper(sources[i], destinations[i]);
            }
        }

        private Action<TSource, TDestination> GetMapper<TSource, TDestination>()
        {
            var sourceType = typeof(TSource);
            var destinationType = typeof(TDestination);

            var cacheKey = $"{sourceType.FullName}_{destinationType.FullName}";
            
            return (Action<TSource, TDestination>)_mapperCache[cacheKey];
        }
    }

    public class MemberConfiguration<TSource>
    {
        public bool IsIgnore { get; private set; }

        public LambdaExpression CustomMapper { get; private set; }

        public Type SourceType { get; set; }

        public Type DestinationType { get; set; }

        public void UseValue<TValue>(TValue value)
        {
            MapFrom(src => value);
        }

        public void Ignore()
        {
            IsIgnore = true;
        }

        public void MapFrom<TProperty>(Expression<Func<TSource, TProperty>> source)
        {
            CustomMapper = source;
            SourceType = typeof(TProperty);
        }
    }

    public interface IMappingExpression
    {
        object CreateDelegate();
    }

    public class MappingExpression<TSource, TDestination> : IMappingExpression
    {
        public MappingExpression()
        {
            var sourceType = typeof(TSource);
            var destinationType = typeof(TDestination);

            foreach (var destination in destinationType.GetProperties().Where(x => x.CanWrite))
            {
                var source = sourceType.GetProperty(destination.Name);

                if (source == null || !source.CanRead)
                {
                    continue;
                }

                _members.Add(destination.Name, new MemberConfiguration<TSource>
                {
                    SourceType = source.PropertyType,
                    DestinationType = destination.PropertyType
                });
            }
        }

        private readonly Dictionary<string, MemberConfiguration<TSource>> _members = new Dictionary<string, MemberConfiguration<TSource>>();

        public MappingExpression<TSource, TDestination> ForMember<TProperty>(Expression<Func<TDestination, TProperty>> destinationMember, Action<MemberConfiguration<TSource>> memberOptions)
        {
            var memberName = ((MemberExpression)destinationMember.Body).Member.Name;

            if (!_members.ContainsKey(memberName))
            {
                _members.Add(memberName, new MemberConfiguration<TSource>
                {
                    DestinationType = typeof(TProperty)
                });
            }

            memberOptions(_members[memberName]);

            return this;
        }

        public object CreateDelegate()
        {
            var sourceParameter = Expression.Parameter(typeof(TSource), "source");
            var destinationParameter = Expression.Parameter(typeof(TDestination), "destination");

            var assigns = new List<Expression>();

            foreach (var member in _members.Where(x => !x.Value.IsIgnore))
            {
                var source = member.Value.CustomMapper == null ? (Expression)Expression.Property(sourceParameter, member.Key) : Expression.Invoke(member.Value.CustomMapper, sourceParameter);
                var destination = Expression.Property(destinationParameter, member.Key);

                var sourceType = member.Value.SourceType;
                var destinationType = member.Value.DestinationType;

                if (sourceType == destinationType)
                {
                    assigns.Add(Expression.Assign(destination, source));
                }
                else
                {
                    var sourceUnderlyingType = Nullable.GetUnderlyingType(member.Value.SourceType);
                    var destinationUnderlyingType = Nullable.GetUnderlyingType(member.Value.DestinationType);

                    if (sourceUnderlyingType != null && destinationUnderlyingType == null)
                    {
                        assigns.Add(Expression.Assign(destination, Expression.Call(source, "GetValueOrDefault", null)));
                    }
                    else if (sourceUnderlyingType == null && destinationUnderlyingType != null)
                    {
                        assigns.Add(ExpressionHelper.IfAssign(ExpressionHelper.IsNotNull(source), destination, ExpressionHelper.ChangeType(source, destinationType)));
                    }
                    else if (typeof(IConvertible).IsAssignableFrom(destinationType) && typeof(IConvertible).IsAssignableFrom(sourceType))
                    {
                        assigns.Add(Expression.Assign(destination, ExpressionHelper.ChangeType(source, destinationType)));
                    }
                }
            }

            var lambda = Expression.Lambda<Action<TSource, TDestination>>(Expression.Block(assigns), sourceParameter, destinationParameter);

            return lambda.Compile();
        }
    }

    public static class ExpressionHelper
    {
        private static readonly MethodInfo _changeTypeMethod = typeof(Convert).GetMethod("ChangeType", new[] { typeof(object), typeof(Type) });

        public static Expression IsNull(Expression value)
        {
            // (object)value == null
            return Expression.Equal(Expression.Convert(value, typeof(object)), Expression.Constant(null));
        }

        public static Expression IsNotNull(Expression value)
        {
            // (object)value != null
            return Expression.NotEqual(Expression.Convert(value, typeof(object)), Expression.Constant(null));
        }

        public static Expression ChangeType(Expression value, Type type)
        {
            // return (TargetType)Convert.ChangeType((object)value, typeof(TargetType))
            return Expression.Convert(Expression.Call(_changeTypeMethod, Expression.Convert(value, typeof(object)), Expression.Constant(Nullable.GetUnderlyingType(type) ?? type)), type);
        }

        public static Expression IfAssign(Expression test, Expression left, Expression right)
        {
            // if (test) { left = right }
            return Expression.IfThen(test, Expression.Assign(left, right));
        }
    }

    public class MapperConfiguration
    {
        public MapperConfiguration(Action<MapperConfiguration> config)
        {
            config(this);
        }

        private readonly Dictionary<string, IMappingExpression> _mappings = new Dictionary<string, IMappingExpression>();

        public MappingExpression<TSource, TDestination> CreateMap<TSource, TDestination>()
        {
            var mapping = new MappingExpression<TSource, TDestination>();

            var sourceType = typeof(TSource);
            var destinationType = typeof(TDestination);

            var cacheKey = $"{sourceType.FullName}_{destinationType.FullName}";

            _mappings.Add(cacheKey, mapping);

            return mapping;
        }

        public Mapper CreateMapper()
        {
            return new Mapper(_mappings.ToDictionary(x => x.Key, x => x.Value.CreateDelegate()));
        }
    }
}