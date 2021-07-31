using EQLogParser.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Xunit;

namespace EQLogParserTests.Helpers
{
    public class FileServiceTests
    {
        [Fact]
        public void Exists_Found()
        {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            var files = new FileService(Path.GetDirectoryName(assembly.Location));
            Assert.True(files.Exists("EQLogParserTests.dll"));
        }

        [Fact]
        public void Exists_AllowTrailingPathSeparatorInRoot()
        {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            var files = new FileService(Path.GetDirectoryName(assembly.Location) + Path.DirectorySeparatorChar);
            Assert.True(files.Exists("EQLogParserTests.dll"));
        }

        [Fact]
        public void Exists_NotFound()
        {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            var files = new FileService(Path.GetDirectoryName(assembly.Location));
            Assert.False(files.Exists("AFileThatDoesntExist.txt"));
        }

        [Fact]
        public void Exists_RejectAbsolutePath()
        {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            var files = new FileService(Path.GetDirectoryName(assembly.Location));
            // this file probably always exists but access to it should be blocked because it 
            // is outside the root folder of the FileService
            Assert.False(files.Exists("c:/windows/system.ini"));
        }


    }
}
