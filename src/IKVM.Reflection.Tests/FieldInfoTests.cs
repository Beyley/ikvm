using System;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IKVM.Reflection.Tests
{

    [TestClass]
    public class FieldInfoTests
    {

        [TestMethod]
        public void FunctionPtrShouldBeIntPtr()
        {
            var t = typeof(DateTime);
            var tf = t.GetField("s_pfnGetSystemTimeAsFileTime", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            var u = new IKVM.Reflection.Universe();
            var ia = u.LoadFile(t.Assembly.Location);
            var it = ia.GetType("System.DateTime");
            var itf = it.GetField("s_pfnGetSystemTimeAsFileTime", BindingFlags.NonPublic | BindingFlags.Static);
        }

    }

}
