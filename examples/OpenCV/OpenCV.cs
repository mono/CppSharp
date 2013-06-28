using CppSharp.Generators;
using CppSharp.Passes;

namespace CppSharp
{
    class OpenCV : ILibrary
    {
        public void Setup(Driver driver)
        {
            var options = driver.Options;
            options.LibraryName = "OpenCV";
            options.Headers.Add("opencv2/core/core_c.h");
            options.Headers.Add("opencv2/core/types_c.h");
            options.IncludeDirs.Add("../../../examples/OpenCV/opencv/include/");
            options.IncludeDirs.Add("../../../examples/OpenCV/opencv/modules/core/include");
            options.IncludeDirs.Add("../../../examples/OpenCV/opencv/modules/flann/include");
            options.IncludeDirs.Add("../../../examples/OpenCV/opencv/modules/imgproc/include");
            options.IncludeDirs.Add("../../../examples/OpenCV/opencv/modules/photo/include");
            options.IncludeDirs.Add("../../../examples/OpenCV/opencv/modules/video/include");
            options.IncludeDirs.Add("../../../examples/OpenCV/opencv/modules/features2d/include");
            options.IncludeDirs.Add("../../../examples/OpenCV/opencv/modules/objdetect/include");
            options.IncludeDirs.Add("../../../examples/OpenCV/opencv/modules/calib3d/include");
            options.IncludeDirs.Add("../../../examples/OpenCV/opencv/modules/ml/include");
            options.IncludeDirs.Add("../../../examples/OpenCV/opencv/modules/highgui/include");
            options.IncludeDirs.Add("../../../examples/OpenCV/opencv/modules/contrib/include");
            options.OutputDir = "opencv";
        }

        public void Preprocess(Driver driver, Library lib)
        {
        }

        public void Postprocess(Library lib)
        {

        }

        public void SetupPasses(Driver driver, PassBuilder p)
        {
            p.FunctionToInstanceMethod();
            p.FunctionToStaticMethod();
        }

        public void GenerateStart(TextTemplate template)
        {
        }

        public void GenerateAfterNamespaces(TextTemplate template)
        {
        }

        static class Program
        {
            public static void Main(string[] args)
            {
                ConsoleDriver.Run(new OpenCV());
            }
        }
    }
}
