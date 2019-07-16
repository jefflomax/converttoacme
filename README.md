# Convert To ACME
Convert 6502 Assembler source from
* 2500AD Cross Assembler
* Commodore Macro Assembler
* Programmers Development System (PDS)\
to ACME Cross Assembler.

## Written in .NET Core 3, runnable on Windows 10, MacOS, and Linux

### This is a simple line-by-line converter designed to read 6502 Assembly Language source and emit source that can be assembled using the ACME cross assembler.
Visual Studio Code has an excellent extension for working with the ACME Cross assembler.   [ACME VS Code Extension](https://marketplace.visualstudio.com/items?itemName=TonyLandi.acmecrossassembler)

In addition, this template [Ingo Hinterding Assemble 6502 in VS Code](https://github.com/Esshahn/acme-assembly-vscode-template) makes it easy.

The conversion does not support all features, just enough to get my source code translated.  The most signification limitations are:
* Very little support for macros, only simple parameterless macros in PDS right now
* The translator has no knowledge of the state of conditionals
* The ACME cross assembler has no support for concepts such as PUBLIC and EXTERNAL
* BIT7 directives are supported for strings, but no DC support

## Converting .ASM files
convertoacme.exe INPUT.ASM conversion.json > OUTPUT.ASM

Or using a simple batch:
for %%i in (*.ASM) do converttoacme.exe %%1 conversion.json > converted\%%1

## conversion.json
See the samplesetting folder for some simple examples
```
{
"Format":"AD2500 | MADS | PDS",
"Modules":[
  {
    "File":"FILENAME.ASM",
    "abr":"XX", // Prefix to uniquify a colliding symbol
    "Renames": [
      "SYMBOL_TO_UNIQUIFY", . . .
    ]
    }, . . .
],
"Macros":["MacroName", . . . ],
"EmitOriginal":true // Emit a copy of modified lines as a comment
}
```

## Other helpful tools
If you have a binary of your original program, comparing the original to ACME's
build in a good hex diff tool can really help.

[VBinDiff by Christopher J. Madsen helped me a lot](https://www.cjmweb.net/vbindiff/)
