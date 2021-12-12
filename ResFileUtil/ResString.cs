using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ResFileUtil
{
  public class ResString
  {
    public string Text     { get; set; }
    public UInt32 ResId    { get; set; }
    public UInt32 LocaleId { get; set; }
    public UInt32 CombinedID => ( LocaleId << 16 ) | ResId;
    public int    Length => Text.Length;                      // Is there any need for this???
  }
}
