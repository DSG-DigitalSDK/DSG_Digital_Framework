using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DSG.Drivers.Siemens
{

    public enum S7PlcArea
    {
        PE,
        PA,
        MK,
        DB,
        CT,
        TM
    }

    public class S7PlcDataItem
    {
        public S7PlcArea Area { get; set; } = S7PlcArea.DB;
        public int DbNum { get; set; }
        public int Offset { get; set; }
        public int Length { get; set; }
        public byte[]? Data { get; set; }

        public override string ToString()
        {
            if (Area == S7PlcArea.DB)
                return $"{Area}:DB={DbNum}:Offset={Offset}:Size={Length}";
            return $"{Area}:Offset={Offset}:Size={Length}";
        }

        static public S7PlcDataItem Create(S7PlcArea area, int dbNum, int offset, int length)
        {
            if (length <= 0)
                return null;
            return new S7PlcDataItem()
            {
                Area = area,
                Data = new byte[length],
                DbNum = dbNum,
                Length = length,
                Offset = offset
            };
        }
    }
}
