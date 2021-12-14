using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ResFileUtil
{
  public class ResStrings : List<ResString>
  {
    // The first C# version will not support the dirty flag.
    // Let's see whether we need it first.
    // internal bool Dirty { get; set; }

    public void         SortByCombinedID()                              => this.Sort ( (x,y) => x.CombinedID.CompareTo(y.CombinedID ) ) ;
    public bool         Exists ( UInt32 LocaleId, UInt32 ResId )        => this.Any ( x => ( x.ResId == ResId ) && ( x.LocaleId == LocaleId ) ) ;
    public List<UInt32> Locales                                         => this.Select ( x => x.LocaleId ).Distinct().ToList() ;
    public List<UInt32> ResIds                                          => this.Select ( x => x.ResId ).Distinct().OrderBy(x => x).ToList() ;
    public ResString    GetByLocaleAndResId ( int LocaleId, int ResId ) => this.FirstOrDefault ( x => ( x.LocaleId == LocaleId ) && ( x.ResId == ResId ) );
  }
}
