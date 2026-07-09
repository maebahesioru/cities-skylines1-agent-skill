using System;
using System.Text;
using ColossalFramework;

namespace SkylinesAgentBridge
{
    /// <summary>
    /// Transfer/logistics state — goods delivery, import/export.
    /// Uses TransferManager.instance.
    /// </summary>
    public static class TransferCommands
    {
        public static CommandResult BuildTransfersJson()
        {
            TransferManager tm = TransferManager.instance;
            if (tm == null) return CommandResult.Fail("TransferManager not found.");

            StringBuilder json = new StringBuilder();
            json.Append("{\"ok\":true");

            // TransferManager fields via reflection
            try
            {
                var fIncomingSize = typeof(TransferManager).GetField("m_incomingCount",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var fOutgoingSize = typeof(TransferManager).GetField("m_outgoingCount",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var fIncomingAmount = typeof(TransferManager).GetField("m_incomingAmount",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var fOutgoingAmount = typeof(TransferManager).GetField("m_outgoingAmount",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                // Try getting counts from TransferManager
                // Alternative: count transfer offers
                var fInOffer = typeof(TransferManager).GetField("m_incomingOffers",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var fOutOffer = typeof(TransferManager).GetField("m_outgoingOffers",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                json.Append(",\"transferStats\":{");
                if (fInOffer != null)
                {
                    var inOffers = fInOffer.GetValue(tm) as Array;
                    json.Append("\"incomingOffers\":" + (inOffers != null ? inOffers.Length : 0));
                }
                else
                {
                    json.Append("\"incomingOffers\":-1");
                }
                json.Append(",");
                if (fOutOffer != null)
                {
                    var outOffers = fOutOffer.GetValue(tm) as Array;
                    json.Append("\"outgoingOffers\":" + (outOffers != null ? outOffers.Length : 0));
                }
                else
                {
                    json.Append("\"outgoingOffers\":-1");
                }
                json.Append("}");

                // Per-material transfer amounts
                json.Append(",\"materials\":{");
                try
                {
                    var fInAmount = typeof(TransferManager).GetField("m_incomingAmount",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    var fOutAmount = typeof(TransferManager).GetField("m_outgoingAmount",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                    if (fInAmount != null)
                    {
                        int[] inAmt = fInAmount.GetValue(tm) as int[];
                        if (inAmt != null)
                        {
                            for (int i = 0; i < Math.Min(inAmt.Length, 20); i++)
                            {
                                if (i > 0) json.Append(",");
                                json.Append("\"" + GetMaterialName(i) + "In\":" + inAmt[i]);
                            }
                        }
                    }
                    if (fOutAmount != null)
                    {
                        int[] outAmt = fOutAmount.GetValue(tm) as int[];
                        if (outAmt != null)
                        {
                            for (int i = 0; i < Math.Min(outAmt.Length, 20); i++)
                            {
                                json.Append(",\"" + GetMaterialName(i) + "Out\":" + outAmt[i]);
                            }
                        }
                    }
                }
                catch { }
                json.Append("}");
            }
            catch { json.Append(",\"transferStats\":{},\"materials\":{}"); }

            json.Append("}");
            return CommandResult.FromJson(json.ToString());
        }

        private static string GetMaterialName(int index)
        {
            switch (index)
            {
                case 0: return "goods";
                case 1: return "food";
                case 2: return "lumber";
                case 3: return "coal";
                case 4: return "oil";
                case 5: return "petrol";
                case 6: return "ore";
                case 7: return "agriculture";
                case 8: return "fish";
                default: return "material" + index;
            }
        }
    }
}
