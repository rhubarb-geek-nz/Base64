// Copyright (c) 2024 Roger Brown.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Management.Automation;
using System.Security.Cryptography;
using System.Text;

namespace RhubarbGeekNz.Base64
{
    internal class PSCmdletStringWriter : Stream
    {
        private readonly PSCmdlet cmdlet;
        private readonly byte[] lineBuffer;
        private int lineBufferLength;
        private Encoding encoding = Encoding.ASCII;

        internal PSCmdletStringWriter(PSCmdlet pscmdlet, int maxlen)
        {
            cmdlet = pscmdlet;
            lineBuffer = new byte[maxlen];
        }

        public override bool CanRead => throw new NotImplementedException();

        public override bool CanSeek => throw new NotImplementedException();

        public override bool CanWrite => true;

        public override long Length => throw new NotImplementedException();

        public override long Position { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public override void Flush()
        {
            if (lineBufferLength > 0)
            {
                cmdlet.WriteObject(Encoding.ASCII.GetString(lineBuffer, 0, lineBufferLength));
                lineBufferLength = 0;
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            while (count > 0)
            {
                int len = lineBuffer.Length - lineBufferLength;

                if (len > count)
                {
                    len = count;
                }

                if (len == lineBuffer.Length)
                {
                    cmdlet.WriteObject(encoding.GetString(buffer, offset, len));
                    count -= len;
                    offset += len;
                }
                else
                {
                    Buffer.BlockCopy(buffer, offset, lineBuffer, lineBufferLength, len);

                    offset += len;
                    count -= len;
                    lineBufferLength += len;

                    if (lineBufferLength == lineBuffer.Length)
                    {
                        cmdlet.WriteObject(encoding.GetString(lineBuffer));
                        lineBufferLength = 0;
                    }
                }
            }
        }
    }

    [Cmdlet(VerbsData.ConvertTo, "Base64")]
    [OutputType(typeof(string))]
    sealed public class ConvertToBase64 : PSCmdlet, IDisposable
    {
        [Parameter(Mandatory = true, ValueFromPipeline = true, HelpMessage = "Binary Data")]
        [AllowNull()]
        [AllowEmptyCollection()]
        public byte[] Value;

        [Parameter(Mandatory = false, HelpMessage = "Maximum String Length")]
        public int Length = 64;

        private Stream writer;

        protected override void BeginProcessing()
        {
            if (Length > 0)
            {
                writer = new CryptoStream(new PSCmdletStringWriter(this, Length), new ToBase64Transform(), CryptoStreamMode.Write);
            }
            else
            {
                WriteError(new ErrorRecord(new IndexOutOfRangeException("Length must be larger than zero"), "Length", ErrorCategory.InvalidArgument, null));
            }
        }

        protected override void ProcessRecord()
        {
            if (Value != null && Value.Length > 0)
            {
                writer.Write(Value, 0, Value.Length);
            }
        }

        protected override void EndProcessing()
        {
            writer.Close();
        }

        public void Dispose()
        {
            IDisposable disposable = writer;
            writer = null;
            if (disposable != null)
            {
                disposable.Dispose();
            }
        }
    }
}
