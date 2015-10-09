﻿using System;
using System.IO;
using System.Text;

namespace Laser.Orchard.Accessibility
{
    public sealed class StringWriterWithEncoding : StringWriter
    {
        private readonly Encoding encoding;

        public StringWriterWithEncoding(Encoding encoding, IFormatProvider formatProvider)
            : base(formatProvider)
        {
            this.encoding = encoding;
        }

        public override Encoding Encoding
        {
            get { return encoding; }
        }
    }
}