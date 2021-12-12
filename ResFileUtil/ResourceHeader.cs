using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ResFileUtil
{
  //
  // struct ResourceHeader
  //
  // The resourse header cannot always be defined as a C structure,
  // because two of the fields may have a variable length.
  // HOWEVER, 
  // - for the special case of string resources, these fields have 
  //   a fixed length and
  // - for other resource types, we only require the first two fields
  //   which are always at fixed positions.
  //
  internal class ResourceHeader
  {
    public const UInt16 RT_STRING = 6 ;
    // In C++ it was a no brainer to use sizeof on the structure. Those were the days.
    public const UInt32 SIZEOF_HEADER_FOR_STRING_RESOURCES = 32 ;

    public UInt32     DataSize;           // Size of data without header
    public UInt32     HeaderSize;         // Length of the additional header
    // [Ordinal or name TYPE]             // Type identifier, id or string
    public UInt16     TypeNameDummy ;     // For String resourses 0xFFFF
    public UInt16     TypeId ;
    //[Ordinal or name NAME]              // Name identifier, id or string
    public UInt16     IdNameDummy ;       // For String resourses 0xFFFF
    public UInt16     Id ;
    public UInt32     DataVersion;        // Predefined resource data version
    public UInt16     MemoryFlags;        // State of the resource
    public UInt16     LanguageId;         // Unicode support for NLS
    public UInt32     Version;            // Version of the resource data
    public UInt32     Characteristics;    // Characteristics of the data

    public void StoreToStream ( BinaryWriter o )
    {
      Debug.Assert ( HeaderSize == SIZEOF_HEADER_FOR_STRING_RESOURCES ) ;
      o.Write ( DataSize ) ;           
      o.Write ( HeaderSize ) ;         
      o.Write ( TypeNameDummy  ) ;     
      o.Write ( TypeId  ) ;
      o.Write ( IdNameDummy  ) ;       
      o.Write ( Id  ) ;
      o.Write ( DataVersion ) ;        
      o.Write ( MemoryFlags ) ;        
      o.Write ( LanguageId ) ;         
      o.Write ( Version ) ;            
      o.Write ( Characteristics ) ;    
    }
  }
}
