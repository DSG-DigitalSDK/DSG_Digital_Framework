using DSG.Log;
using DSG.Base;
using Sharp7;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace DSG.Drivers.Siemens
{
    public static class S7DataConversion
    {
        static string sC = nameof(S7DataConversion);
        public static S7Area ToS7Area(S7PlcArea eArea)
        {
            string sM = nameof(ToS7Area);
            switch (eArea)
            {
                case S7PlcArea.CT: return S7Area.CT;
                case S7PlcArea.DB: return S7Area.DB;
                case S7PlcArea.MK: return S7Area.MK;
                case S7PlcArea.PA: return S7Area.PA;
                case S7PlcArea.PE: return S7Area.PE;
                case S7PlcArea.TM: return S7Area.TM;
                default: throw new ArgumentException($"Invalid {nameof(S7PlcArea)} '{eArea}'");
            }
        }

        public static S7PlcArea ToPlcArea(S7Area eArea)
        {
            string sM = nameof(ToPlcArea);
            switch (eArea)
            {
                case S7Area.CT: return S7PlcArea.CT;
                case S7Area.DB: return S7PlcArea.DB;
                case S7Area.MK: return S7PlcArea.MK;
                case S7Area.PA: return S7PlcArea.PA;
                case S7Area.PE: return S7PlcArea.PE;
                case S7Area.TM: return S7PlcArea.TM;
                default: throw new ArgumentException($"Invalid {nameof(S7Area)} '{eArea}'");
            }
        }

        public static S7Client.S7DataItem? ToS7DataItem(S7PlcDataItem oData)
        {
            string sM = nameof(ToS7DataItem);
            try
            {
                if (oData == null || oData.Length == 0)
                {
                    LogMan.Error(sC, sM, $"Invalid Data : '{oData?.ToString() ?? "null"}'");
                    return null;
                }
                var eArea = S7DataConversion.ToS7Area(oData.Area);
                switch (eArea)
                {
                    case S7Area.DB:
                        {
                            return new S7Client.S7DataItem()
                            {
                                Amount = oData.Length,
                                WordLen = (int)S7WordLength.Byte,// S7Consts.S7WLByte,
                                Area = (int)eArea,
                                DBNumber = oData.DbNum,
                                Start = oData.Offset,
                                pData = oData.Data == null ? IntPtrHandler.Create(oData.Length).Handle : IntPtrHandler.Create(oData.Data).Handle
                            };
                        }
                    default:
                        {
                            LogMan.Error(sC, sM, $"Area {oData.Area} management not yet supported");
                            return null;
                        }
                }
            }
            catch (Exception ex)
            {
                LogMan.Exception(sC, sM, ex);
                return null;
            }
        }

        

        public static S7PlcDataItem? ToPlcDataItem(S7Client.S7DataItem oItem)
        {

            string sM = nameof(ToPlcDataItem);
            try
            {
                if (oItem.Amount == 0)
                {
                    LogMan.Error(sC, sM, $"Invalid S7DataItem'");
                    return null;
                }
                var eS7Area = (S7Area)oItem.Area;
                switch (eS7Area)
                {
                    case S7Area.DB:
                        {
                            var oData = new S7PlcDataItem()
                            {
                                Area = S7PlcArea.DB,
                                DbNum = oItem.DBNumber,
                                Offset = oItem.Start,
                                Length = oItem.Amount,
                                oData = new byte[oItem.Amount]
                            };
                            Marshal.Copy( oItem.pData, oData.Data, 0, oItem.Amount);
                            return oData;
                        }
                    default:
                        {
                            LogMan.Error(sC, sM, $"Area {eS7Area} management not yet supported");
                            return null;
                        }
                }
            }
            catch (Exception ex)
            {
                LogMan.Exception(sC, sM, ex);
                return null;
            }
        }
    }
}
