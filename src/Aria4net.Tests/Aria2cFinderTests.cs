using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Aria4net.Common;
using Aria4net.Server;
using NUnit.Framework;

namespace Aria4net.Tests
{
    [TestFixture]
    public class Aria2cFinderTests
    {
        [Test]
        public void Can_find_file()
        {
            string currentAssemblyPath = Assembly.GetExecutingAssembly().Location;
            IFileFinder finder = new Aria2cFinder(new Aria2cConfig
                {
                    Executable = currentAssemblyPath
                });

            string path = finder.Find();

            Assert.That(path,
                Is.EqualTo(currentAssemblyPath));
        }

        [Test]
        public void If_file_doesnt_exists_throws_exception()
        {
            IFileFinder finder = new Aria2cFinder(new Aria2cConfig
            {
                Executable = "invalid path"
            });

            Assert.That(() => finder.Find(), 
                Throws.Exception.InstanceOf<FileNotFoundException>());
            
        }
    }
}
