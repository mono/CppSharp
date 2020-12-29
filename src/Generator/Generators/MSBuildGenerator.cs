using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CppSharp.AST;

namespace CppSharp.Generators
{
    public class MSBuildGenerator : CodeGenerator
    {
        public MSBuildGenerator(BindingContext context, Module module,
            Dictionary<Module, string> libraryMappings)
            : base(context)
        {
            this.module = module;
            this.libraryMappings = libraryMappings;
        }

        public override string FileExtension => "csproj";

        public override void Process()
        {
            var location = System.Reflection.Assembly.GetExecutingAssembly().Location;
            Write($@"
<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>netstandard2.0</TargetFramework>
    <PlatformTarget>{(Context.TargetInfo.PointerWidth == 64 ? "x64" : "x86")}</PlatformTarget>
    <OutputPath>{Options.OutputDir}</OutputPath>
    <DocumentationFile>{module.LibraryName}.xml</DocumentationFile>
    <Configuration>Release</Configuration>
    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
    <EnableDefaultNoneItems>false</EnableDefaultNoneItems>
    <EnableDefaultItems>false</EnableDefaultItems>
    <AppendTargetFrameworkToOutputPath>false</AppendTargetFrameworkToOutputPath>
  </PropertyGroup>
  <ItemGroup>
    {string.Join(Environment.NewLine, module.CodeFiles.Select(c =>
  $"<Compile Include=\"{c}\" />"))}
  </ItemGroup>
  <ItemGroup>
    {string.Join(Environment.NewLine,
         new[] { Path.Combine(Path.GetDirectoryName(location), "CppSharp.Runtime.dll") }
         .Union(module.Dependencies.Where(libraryMappings.ContainsKey).Select(d => libraryMappings[d]))
         .Select(reference =>
 $@"<Reference Include=""{Path.GetFileNameWithoutExtension(reference)}"">
      <HintPath>{reference}</HintPath>
    </Reference>"))}
  </ItemGroup>
</Project>".Trim());
        }

        private readonly Module module;
        private readonly Dictionary<Module, string> libraryMappings;
    }
}
