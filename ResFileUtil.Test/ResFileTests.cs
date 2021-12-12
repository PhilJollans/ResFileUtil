using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using ResFileUtil;

namespace ResFileUtil.Test
{
  [TestClass]
  public class ResFileTests
  {
    //
    // This is a method with which I have debugged the code, but it is in no way a real Unit-Test.
    //
    [TestMethod]
    public void ImportFile()
    {
      var f = new ResFile() ;
      f.Filename = @"C:\MultiLang_VB6\AddIn\MultiLang.res";
      f.Import() ;
    }

    //
    // This is a method with which I have debugged the code, but it is in no way a real Unit-Test.
    //
    [TestMethod]
    public void ExportFile()
    {
      const string tempname = @"C:\Temp\MultiLang.res" ;

      File.SetAttributes ( tempname, FileAttributes.Normal );
      File.Delete ( tempname );
      File.Copy ( @"C:\MultiLang_VB6\AddIn\MultiLang.res" , tempname ) ;

      var f = new ResFile() ;
      f.Filename = tempname;
      f.Import ();
      f.Export ();
    }

    //
    // This is a method with which I have debugged the code, but it is in no way a real Unit-Test.
    //
    [TestMethod]
    public void SaveFile()
    {
      var f = new ResFile() ;
      f.Filename = @"C:\MultiLang_VB6\AddIn\MultiLang.res";
      f.Import ();

      const string tempname = @"C:\Temp\TestOutput.res" ;

      if ( File.Exists ( tempname ) )
      {
        File.SetAttributes ( tempname, FileAttributes.Normal );
        File.Delete ( tempname ) ;
      }

      f.Filename = tempname;
      f.SaveAsNewFile () ;
    }

  }
}
