#!/usr/bin/env pwsh
# Copyright (c) 2024 Roger Brown.
# Licensed under the MIT License.

trap
{
	throw $PSItem
}

$ErrorActionPreference = 'Stop'
$InformationPreference = 'Continue'

Get-Command -Noun 'Base64'

$bytes = [System.Text.Encoding]::ASCII.GetBytes('Hello World')

$base64 = @(,$bytes) | ConvertTo-Base64

[psobject]@{
	Base64=$base64
} | Format-Table

$result = $base64 | ConvertFrom-Base64

$bytes,$result | Format-Hex
