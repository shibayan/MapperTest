using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapperTest
{
    class MyClass
    {
        public int P1 { get; set; }
        public int P2 { get; set; }
        public int P3 { get; set; }

        public string P4 { get; set; }
        public string P5 { get; set; }
        public string P6 { get; set; }

        public DateTime P7 { get; set; }
        public DateTime P8 { get; set; }
        public DateTime P9 { get; set; }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Do(NormalTest);

            Do(StandardMapperTest);

            Watch(() => AutoMapper.Mapper.CreateMap<MyClass, MyClass>());
            Do(AutoMapperTest);

            Watch(() => ExpressMapper.Mapper.Register<MyClass, MyClass>());
            Do(ExpressMapperTest);

            Do(DescriptorMapperTest);

            Do(ExpressionMapperTest);

            Watch(MicroMapperPrepare);
            Do(MicroMapperTest);


            Console.WriteLine("\n << end >>");
            Console.Read();
        }

        static void Do(Action<MyClass, MyClass> action, long count = 500000)
        {
            Console.WriteLine("{0} >>>", action.Method.Name);

            var src = new MyClass
            {
                P1 = 1,
                P2 = 1,
                P3 = 1,
                P4 = "aaaa",
                P5 = "aaaa",
                P6 = "aaaa",
                P7 = new DateTime(),
                P8 = new DateTime(),
                P9 = new DateTime()
            };
            var dst = new MyClass();

            // for caching model mapping..
            action(src, dst);

            Watch(() => action(src, dst), count);

            Console.WriteLine("{0} <<<\n", action.Method.Name);
        }

        static void Watch(Action action, long count = 1)
        {
            var sw = Stopwatch.StartNew();
            for (long i = 0; i < count; i++)
                action();
            sw.Stop();

            Console.WriteLine("{0}", sw.Elapsed);
        }

        static void NormalTest(MyClass src, MyClass dst)
        {
            dst.P1 = src.P1;
            dst.P2 = src.P2;
            dst.P3 = src.P3;
            dst.P4 = src.P4;
            dst.P5 = src.P5;
            dst.P6 = src.P6;
            dst.P7 = src.P7;
            dst.P8 = src.P8;
            dst.P9 = src.P9;
        }

        static void StandardMapperTest(MyClass src, MyClass dst)
        {
            StandardMapper.Map(src, dst);
        }

        static void AutoMapperTest(MyClass src, MyClass dst)
        {
            AutoMapper.Mapper.Map(src, dst);
        }

        static void ExpressMapperTest(MyClass src, MyClass dst)
        {
            ExpressMapper.Mapper.Map(src, dst);
        }

        static void DescriptorMapperTest(MyClass src, MyClass dst)
        {
            DescriptorMapper.Map(src, dst);
        }

        static void ExpressionMapperTest(MyClass src, MyClass dst)
        {
            ExpressionMapper.Map(src, dst);
        }

        private static MicroMapper.Mapper _mapper;

        static void MicroMapperPrepare()
        {
            var config = new MicroMapper.MapperConfiguration(cfg =>
            {
                cfg.CreateMap<MyClass, MyClass>();
            });

            _mapper = config.CreateMapper();
        }

        static void MicroMapperTest(MyClass src, MyClass dst)
        {
            _mapper.Map(src, dst);
        }
    }
}
