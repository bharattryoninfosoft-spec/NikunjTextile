using System;

namespace NikunjTextile.Class
{
    public static class NumberToWordsHelper
    {
        private static readonly string[] Units =
        {
            "", "One", "Two", "Three", "Four", "Five", "Six",
            "Seven", "Eight", "Nine", "Ten", "Eleven", "Twelve",
            "Thirteen", "Fourteen", "Fifteen", "Sixteen",
            "Seventeen", "Eighteen", "Nineteen"
        };

        private static readonly string[] Tens =
        {
            "", "", "Twenty", "Thirty", "Forty", "Fifty",
            "Sixty", "Seventy", "Eighty", "Ninety"
        };

        public static string ConvertAmount(decimal amount)
        {
            long rupees = (long)Math.Floor(amount);
            int paise = (int)Math.Round((amount - rupees) * 100);

            string words = NumberToWords(rupees) + " Rupees";

            if (paise > 0)
            {
                words += " and " + NumberToWords(paise) + " Paise";
            }

            return words + " Only";
        }

        private static string NumberToWords(long number)
        {
            if (number == 0)
                return "Zero";

            string words = "";

            if ((number / 10000000) > 0)
            {
                words += NumberToWords(number / 10000000) + " Crore ";
                number %= 10000000;
            }

            if ((number / 100000) > 0)
            {
                words += NumberToWords(number / 100000) + " Lakh ";
                number %= 100000;
            }

            if ((number / 1000) > 0)
            {
                words += NumberToWords(number / 1000) + " Thousand ";
                number %= 1000;
            }

            if ((number / 100) > 0)
            {
                words += NumberToWords(number / 100) + " Hundred ";
                number %= 100;
            }

            if (number > 0)
            {
                if (number < 20)
                {
                    words += Units[number];
                }
                else
                {
                    words += Tens[number / 10];
                    if ((number % 10) > 0)
                        words += " " + Units[number % 10];
                }
            }

            return words.Trim();
        }
    }
}
