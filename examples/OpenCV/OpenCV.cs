using Cxxi.Generators;
using Cxxi.Passes;

namespace Cxxi
{
    class OpenCV : ILibrary
    {
        public void Setup(DriverOptions options)
        {
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

        public void Preprocess(Library lib)
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
                Driver.Run(new OpenCV());
            }
        }
    }
}
