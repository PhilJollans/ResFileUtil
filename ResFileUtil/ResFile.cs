using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ResFileUtil
{
  public class ResFile
  {
    public string        Filename { get; set; }
    public ResStrings    Strings { get; } = new ResStrings();
    public List<UInt32>  Locales => Strings?.Locales ;

    public void Import()
    {
      try
      {
        Strings.Clear();

        using ( var s = new FileStream ( Filename, FileMode.Open, FileAccess.Read ) )
        using ( var b = new BinaryReader ( s ) )
        {
          while ( b.BaseStream.Position < b.BaseStream.Length )
          {
            import_single_resource ( b ) ;
          }
        }
      }
      catch ( EndOfStreamException ex )
      {
        throw new Exception ( "Unexpected end of file reading resource file", ex ) ;
      }
      catch ( ObjectDisposedException ex )
      {
        throw new Exception ( "Stream closed unexpectedly reading resource file", ex ) ;
      }
      catch ( IOException ex )
      {
        throw new Exception ( "I/O error reading resource file", ex ) ;
      }
      catch ( Exception ex )
      {
        throw new Exception ( "Exception reading resource file", ex );
      }
    }

    public void Export ( bool PreserveStrings = false )
    {
      // Create a name for the backup file.
      var BaseName = Path.GetFileNameWithoutExtension ( Filename ) ;
      var Folder   = Path.GetDirectoryName ( Filename ) ;
      var BackupName = Path.Combine ( Folder, BaseName + ".bak" ) ;
      Debug.WriteLine ( BackupName ) ;

      if ( File.Exists ( BackupName ) )
      {
        // Remove Read-Only just in case
        File.SetAttributes ( BackupName, FileAttributes.Normal ) ;
        // and delete the file
        File.Delete ( BackupName ) ;
      }

      // Move the res file to the backup
      File.Move ( Filename, BackupName ) ;

      // Open and read the backup file
      using ( var s = new FileStream ( BackupName, FileMode.Open, FileAccess.Read ) )
      using ( var b = new BinaryReader ( s ) )
      {
        // Open the resource file for output
        using ( var d = new FileStream ( Filename, FileMode.Create ) )
        using ( var o = new BinaryWriter ( d ) )
        {
          // Loop through the input file and:
          // - Copy all non string resources to the output file.
          // - If PreserveStrings flag is set, import any strings which
          //   do not conflict with strings already in the collection.
          //   These will later be written to the output file.
          while ( b.BaseStream.Position < b.BaseStream.Length )
          {
            transfer_single_resource ( b, o, PreserveStrings ) ;
          }

          // Common function for actually exporting the strings.
          export_string_resources ( o ) ;
        }
      }

    }

    public void SaveAsNewFile()
    {
      // Open the resource file for output
      using ( var d = new FileStream ( Filename, FileMode.Create ) )
      using ( var o = new BinaryWriter ( d ) )
      {
        // Write an emtpy resource at the start of the file.
        // I havn't found a documented requirement for this, but all 
        // resource files appear to have it.
        export_empty_resource ( o );

        // Common function for actually exporting the strings.
        export_string_resources ( o );
      }
    }

    private void transfer_single_resource ( BinaryReader b, BinaryWriter o, bool PreserveStrings )
    {
      var hdr = new ResourceHeader() ;
      var pos = b.BaseStream.Position ;

      hdr.DataSize      = b.ReadUInt32 ();
      hdr.HeaderSize    = b.ReadUInt32 ();
      hdr.TypeNameDummy = b.ReadUInt16 ();

      // Only continue if the first word of the resource type contains 0xFFFF
      if ( hdr.TypeNameDummy == 0xFFFF )
      {
        hdr.TypeId          = b.ReadUInt16();
        hdr.IdNameDummy     = b.ReadUInt16() ;
        hdr.Id              = b.ReadUInt16() ;
        hdr.DataVersion     = b.ReadUInt32() ;
        hdr.MemoryFlags     = b.ReadUInt16();
        hdr.LanguageId      = b.ReadUInt16() ;
        hdr.Version         = b.ReadUInt32() ;
        hdr.Characteristics = b.ReadUInt32() ;

        // Only continue for string resources
        if ( hdr.TypeId == ResourceHeader.RT_STRING )
        {
          // String resources
          Debug.Assert ( hdr.IdNameDummy == 0xFFFF ) ;

          if ( PreserveStrings )
          {
            conditional_import_string_block ( b, hdr ) ;
          }
        }
        else
        {
          // Not a string resource.
          // Seek back to the start of the header.
          b.BaseStream.Seek ( pos, SeekOrigin.Begin );

          // Read the complete resource
          var buf  = b.ReadBytes ( (int)(hdr.HeaderSize + hdr.DataSize) ) ;
          o.Write ( buf ) ;
        }
      }

      // ALWAYS seek past the header and data to the next block
      var nextpos = pos + hdr.HeaderSize + hdr.DataSize ;
      // pad to DWORD boundary
      nextpos = ( nextpos + 3 ) & ~3;
      b.BaseStream.Seek ( nextpos, SeekOrigin.Begin );
    }


    private void import_single_resource ( BinaryReader b )
    {
      var hdr = new ResourceHeader() ;
      var pos = b.BaseStream.Position ;

      hdr.DataSize      = b.ReadUInt32 ();
      hdr.HeaderSize    = b.ReadUInt32 ();
      hdr.TypeNameDummy = b.ReadUInt16 ();

      // Only continue if the first word of the resource type contains 0xFFFF
      if ( hdr.TypeNameDummy == 0xFFFF )
      {
        hdr.TypeId          = b.ReadUInt16();
        hdr.IdNameDummy     = b.ReadUInt16() ;
        hdr.Id              = b.ReadUInt16() ;
        hdr.DataVersion     = b.ReadUInt32() ;
        hdr.MemoryFlags     = b.ReadUInt16();
        hdr.LanguageId      = b.ReadUInt16() ;
        hdr.Version         = b.ReadUInt32() ;
        hdr.Characteristics = b.ReadUInt32() ;

        // Only continue for string resources
        if ( hdr.TypeId == ResourceHeader.RT_STRING )
        {
          Debug.Assert ( hdr.IdNameDummy == 0xFFFF ) ;
          import_string_block ( b, hdr ) ;
        }
      }

      // seek past the header and data to the next block
      var nextpos = pos + hdr.HeaderSize + hdr.DataSize ;
      // pad to DWORD boundary
      nextpos = ( nextpos + 3 ) & ~3;
      b.BaseStream.Seek ( nextpos, SeekOrigin.Begin );
    }

    private void import_string_block ( BinaryReader b , ResourceHeader hdr )
    {
      for ( int i = 0 ; i < 16 ; i++ )
      {
        var slen = b.ReadUInt16() ;
        if ( slen > 0 )
        {
          // Read the bytes and extract the string
          var buf  = b.ReadBytes ( (int)(slen * 2) ) ;
          var s    = System.Text.Encoding.Unicode.GetString ( buf, 0, buf.Length ) ;

          // Determine the resource ID
          UInt32 ResId = (UInt32)( ( ( hdr.Id - 1) << 4 ) + i ) ;

          // Create our ResString object
          var rs   = new ResString() { LocaleId = hdr.LanguageId, ResId = ResId, Text = s } ;
          Strings.Add ( rs ) ;
        }
      }
    }

    //
    // This is bonkers. Why don't we use this function in all cases and
    // get rid of import_string_block(). There is no case where we want
    // to import duplicates. It will only be a fraction slower.
    //
    private void conditional_import_string_block ( BinaryReader b, ResourceHeader hdr )
    {
      for ( int i = 0 ; i < 16 ; i++ )
      {
        var slen = b.ReadUInt16() ;
        if ( slen > 0 )
        {
          // Read the bytes and extract the string
          var buf  = b.ReadBytes ( (int)(slen * 2) ) ;
          var s    = System.Text.Encoding.Unicode.GetString ( buf, 0, buf.Length ) ;

          // Determine the resource ID
          UInt32 ResId = (UInt32)( ( ( hdr.Id - 1) << 4 ) + i ) ;

          // Do we already have this resource?
          if ( !Strings.Exists ( hdr.LanguageId, ResId))
          {
            // No, then import it
            var rs   = new ResString() { LocaleId = hdr.LanguageId, ResId = ResId, Text = s } ;
            Strings.Add ( rs );
          }
        }
      }
    }

    private void export_string_block ( BinaryWriter o, UInt32 BlockId, ResString[] stringsInBlock )
    {
      Debug.Assert ( stringsInBlock != null );
      Debug.Assert ( stringsInBlock.Length == 16 );

      // Extract the Language ID and top 12 bits of the resource ID.
      UInt32 LangId = ( BlockId >> 16 ) & 0xFFFF;
      UInt32 Top12  = ( ( BlockId >> 4 ) & 0x0FFF ) + 1;

      // Determine the length of the block
      int Size = 0 ;
      for ( int i = 0 ; i < 16 ; i++ )
      {
        // Add one for the string length
        Size++ ;

        // Add the length of the string
        if ( stringsInBlock[i] != null )
        {
          Size += stringsInBlock[i].Length ;
        }
      }

      // Double it, because we are using Unicode characters (and 16 bit lengths)
      Size *= 2;

      // Create a resource header and initialise the fields
      var hdr = new ResourceHeader() ;

      // Initialise all fields in the header.
      hdr.DataSize        = (UInt32)Size ;
      hdr.HeaderSize      = ResourceHeader.SIZEOF_HEADER_FOR_STRING_RESOURCES ;
      hdr.TypeNameDummy   = 0xFFFF ;
      hdr.TypeId          = ResourceHeader.RT_STRING ;
      hdr.IdNameDummy     = 0xFFFF ;
      hdr.Id              = (UInt16)Top12 ;
      hdr.DataVersion     = 0 ;
      hdr.MemoryFlags     = 0 ;
      hdr.LanguageId      = (UInt16)LangId ;
      hdr.Version         = 0 ;
      hdr.Characteristics = 0 ;

      // Write it to the stream
      hdr.StoreToStream ( o ) ;

      // Now write the strings
      for ( int i = 0 ; i < 16 ; i++ )
      {
        UInt16 length = 0 ;
        if ( stringsInBlock[i] != null )
        {
          length = (UInt16)stringsInBlock[i].Length ;
        }

        // Write the length
        o.Write ( length ) ;

        if ( stringsInBlock[i] != null )
        {
          byte[] bytes = Encoding.Unicode.GetBytes ( stringsInBlock[i].Text ) ;
          o.Write ( bytes ) ;
        }
      }

      // Align to DWORD boundary.
      // Note: Padding may also be required at the end of the file to 
      //       make the whole file length a multiple of 4. This is only
      //       effective if we actually write 0's to the stream.
      //       Seeking with output.seekp() works otherwise, but does not
      //       introduce any padding if we never write any more bytes.
      int    pad = ( -Size ) & 3 ;
      for ( int i = 0 ; i < pad ; i++ ) o.Write ( (byte)0 ) ;
    }

    private void export_string_resources ( BinaryWriter o )
    {
      const UInt32 MinusOne = 0xFFFFFFFF ;

      // First sort the strings by the combined ID ( ( LocaleId << 16 ) | ResId ) 
      Strings.SortByCombinedID();

      UInt32            BlockId = MinusOne ;
      UInt32            CombiId ;
      ResString[]       stringsInBlock = null ;

      // The logic of this block is a bit weird, but it is copied from the old C++ version.

      // Now loop over the strings
      foreach ( var rs in Strings )
      {
        CombiId = rs.CombinedID ;

        // Does it belong in the current block?
        if ( ( CombiId & 0xFFFFFFF0 ) != BlockId )
        {
          // No, except for the first block, write it to the file.
          if ( BlockId != MinusOne )
          {
            export_string_block ( o, BlockId, stringsInBlock ) ;
          }

          // Create a new array of 16 strings
          stringsInBlock = new ResString[16] ;

          // Initialise the BlockId for the new block
          BlockId = CombiId & 0xFFFFFFF0;
        }

        Debug.Assert ( stringsInBlock != null ) ;
        Debug.Assert ( stringsInBlock.Length == 16 ) ;

        // Store info about this string in the string block.
        UInt32 FourBitId = CombiId & 0x0F ;
        stringsInBlock[FourBitId]= rs ;
      }

      // Save the final block to the file.
      if ( BlockId != MinusOne )
      {
        export_string_block ( o, BlockId, stringsInBlock ) ;
      }

    }

    private void export_empty_resource ( BinaryWriter o )
    {
      // Create a resource header and initialise the fields
      var hdr = new ResourceHeader() ;

      // Initialise all fields in the header.
      hdr.DataSize        = 0 ;
      hdr.HeaderSize      = ResourceHeader.SIZEOF_HEADER_FOR_STRING_RESOURCES ;
      hdr.TypeNameDummy   = 0xFFFF ;
      hdr.TypeId          = 0 ;
      hdr.IdNameDummy     = 0xFFFF ;
      hdr.Id              = 0 ;
      hdr.DataVersion     = 0 ;
      hdr.MemoryFlags     = 0 ;
      hdr.LanguageId      = 0 ;
      hdr.Version         = 0 ;
      hdr.Characteristics = 0 ;

      // Write it to the stream
      hdr.StoreToStream ( o ) ;
    }

  }
}
