# rhubarb-geek-nz/Base64
Base64 tools for PowerShell

ConvertTo- and ConvertFrom- Base64

Cmdlets works in a pipeline, the input records are treated as one conversion.

The outputs will be broken into records based on the Length argument.

The default output record size for `ConvertTo-Base64` is 64 characters which is consistent with `openssl base64`.

The default output record size for `ConvertFrom-Base64` is 4096 bytes which should be suitable for streaming to disk or network.
