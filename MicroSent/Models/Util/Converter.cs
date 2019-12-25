using MicroSent.Models.Constants;
using MicroSent.Models.Enums;
using System;

namespace MicroSent.Models.Util
{
    public static class Converter
    {
        #region public methods
        public static PosLabels convertTagToPosLabel(string tag)
        {
            tag = tag.Replace(TokenPartConstants.DOLLAR, TokenPartConstants.LETTER_D);
            if (!Enum.TryParse(tag, out PosLabels posLabel))
            {
                return PosLabels.Default;
            }
            return posLabel;
        }
        #endregion
    }
}
