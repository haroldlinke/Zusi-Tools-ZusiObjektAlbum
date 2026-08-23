using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace ZusiObjektAlbum.ValidationRules
{
    class ValidateSectionName : ValidationRule
    {
        public readonly static char[] ForbiddenChars = { '\'', '"', '\r', '\n' };

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            if (value == null)
                return new ValidationResult(true, null);

            if (value is string s)
            {
                if (string.IsNullOrEmpty(s) || s.IndexOfAny(ForbiddenChars) < 0)
                    return new ValidationResult(true, null);
            }

            return new ValidationResult(false, string.Format("Der Bezeichner enthält ungültige Zeichen", value.ToString()));
        }
    }
}
