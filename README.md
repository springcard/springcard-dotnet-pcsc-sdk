# SpringCard .NET PC/SC SDK

This is the SDK published by SpringCard to use PC/SC smart card readers from .NET applications.

Most of the sample applications provided in this SDK as source code as available as ready-to-use binaries in the SpringCard PC/SC QuickStart setup package (SQ1363): [www.springcard.com/download/find/file/sq13163](https://www.springcard.com/en/download/find/file/sq13163)

## Readme first!

### What is PC/SC

PC/SC is the de-facto standard to interface Personal Computers with SmartCards.

PC/SC is implemented by Winscard.dll on Windows and PCSC-lite on Linux and Mac OS.

### Supported devices

Any SpringCard PC/SC coupler may be used with this SDK.

The SDK is tested on all active SpringCard PC/SC platforms, i.e. at least the following products:

- Prox’n’Roll PC/SC HSP, 
- CrazyWriter-HSP,
- Puck One,
- SpringPark.

Earlier CSB6 platform is not maintained anymore, but should work the same in 99.9% of the cases.

Please visit [www.springcard.com](https://www.springcard.com) to see all the devices we offer.

The technical documentation of the products is available on their own page on the main website, on [docs.springcard.com](https://docs.springcard.com/).

### Supported platforms

This SDK targets .NET applications.

Its primary focus is Windows applications, running on the .NET Framework v4.8, but such applications runs on Linux and macOS as well thanks to the Mono runtime.

New platform-agnostic samples, running on .NET Core v6.0, will be added progressively.

### Underlying libraries

The applications in this SDK rely on libraries provided in the [springcard-dotnet-libraries repository](https://github.com/springcard/springcard-dotnet-libraries). In particular, our .NET wrapper for `winscard.dll` (or for the PCSC-Lite subsystem on Linux and the PCSC Framework on macOS), which is the basic component for any PC/SC-aware application, is available in the `SpringCard.PCSC.dll` assembly featured in this repository.

The documentation of all the libraries is available on [docs.springcard.com](https://docs.springcard.com/).

### Going further

Detailed use-case studies and many useful tips are available on our technical blog: [tech.springcard.com](https://tech.springcard.com).

If you're targetting a different plateform, see

- See [github.com/springcard/android-pcsclike/](https://github.com/springcard/android-pcsclike/) for a PC/SC-Like implementation on Android (compatible with all Puck on an USB link, and Puck Blue on a Bluetooth link),
- See [github.com/springcard/ios-pcsclike/](https://github.com/springcard/ios-pcsclike/) for a PC/SC-Like implementation on iOS (compatible with Puck Blue on a Bluetooth link).

## Available applications

### 'Beginner' Visual Basic sample

Directory `projects/beginners/vb` contains a single project named `getCardUid`. This project shows how to add the `SpringCard.PCSC.dll` assembly to a VB project, and how to get a card's UID (protocol-level unique identifier) using the `FFCA000000` APDU.

### 'Beginners' C# samples

Directory `projects/beginners/csharp` contains simple projects showing a few interesting use cases:

* **listReaders**: It’s the simplest example, it lists all the PC/SC readers installed on  the computer. That’s the project to start with.

* **getCardUid**: This program built on the listReaders example, retrieves the readers but also retrieves some data from the inserted card as the ATR, UID, protocol and type.

* **readMifareUltralight**: This project reads all the content of a Mifare Ultralight card.

* **writeMifareUltralight**: This project writes some ASCII (text) data to a Mifare Ultralight card. It only write textual data but  it’s not a limitation of the card.

* **readMifareClassic**: Reads data (only textual data) from a Mifare Classic card.

* **writeMifareClassic**: Writes (textual) data in the card to a specific address.

* **desfireInformation**: Retrieves information from a desFire card. It reads the card’s version and lists the available applications.

* **getReaderInformation**: This project get some information from the reader:

### PcscDiag2

Our reference application, showing how to enumerate the PC/SC readers using `SCardListReaders`, monitor their status using `SCardGetStatusChange` and `SCardStatus`, connect to a reader or a card using `SCardConnect`, exchange smart card commands/responses (APDU) using `SCardTransmit` and control the reader directly using `SCardControl`.

*C#, location is `projects/PcscDiag2`.*

### PcscScriptor

An application to send a list of commands to a smartcard (from a batch file or manual entries).

*C#, location is `projects/PcscScriptor`.*

### NFC Forum samples

#### NfcTagTool

An application to read and write NFC Forum Tags.

Compliant with NFC Forum Type 2 Tags (Mifare UltraLight, NTAG etc) and pre-formatted NFC Forum Type 4 Tags.

*C#, location is `projects/NfcForum/NfcTagTool`.*

#### NfcTagEmul

An application to have the device emulate a NFC Forum Type 4 Tag. Works with Puck only.

*C#, location is `projects/NfcForum/NfcTagEmul`.*

#### NfcBeam

An application to test NFC Forum peer-to-peer exchange (SNEP over LLCP). *P2P is now deprecated and disabled on most smartphones.*

*C#, location is `projects/NfcForum/NfcBeam`.*

### MemoryCardTool

An application to "explore" plain old memory cards like Mifare Classic.

*C#, location is `projects/MemoryCardTool`.*

### Apple VAS and Google SmartTap demos

Some SpringCard PC/SC readers (Prox'N'Roll HSP, Puck and SpringPark) have been certified by Apple to read Apple Wallet NFC passes (Apple VAS with ECP) and by Google to read Smart Tap NFC passes. SpringCard offers libraries to implement the relevant secure transaction with the mobile phones or smart watches to retrieve the content of the pass. The `NfcPass` directory contains

- `PassKitRdr`, a demo that reads Apple Wallet NFC passes
- `SmartTapRdr`, a demo that reads Google Smart Tap NFC passes
- `SpringPassRdr`, a demo that reads both kind of passes.

The underlying libraries are <u>not</u> open source and are subject to a license fee. Therefore, the libraries are available as binaries only. The applications will stop working after 10 minutes unless a valid license string is provided to activate the libraries.

## Building the applications

### Dependencies

First step is to compile the required libraries from repository [springcard-dotnet-libraries repository](https://github.com/springcard/springcard-dotnet-libraries).

You may also download the precompiled binaries from [github.com/springcard/springcard-dotnet-libraries/releases](https://github.com/springcard/springcard-dotnet-libraries/releases). Just download the .ZIP file and extract its content as `_libraries` sub-directory in this repository's main directory.

Alternatively, you may download and install the PC/SC QuickStart (SQ13163) from [www.springcard.com/download/find/file/sq13163](https://www.springcard.com/fr/download/find/file/sq13163) and find the DLL in the installation directory. Copy the libraries in the `_libraries\net48` sub-directory.

### Net48 targets

Every library comes with its project for the Microsoft Visual Studio IDE (.sln and .csproj) in a subdirectory named `net48`.

Projects have been created with Visual Studio 2017, then updated with Visual Studio 2019, and finally have updated with Visual Studio 2022.

Building from the command line is easy using `MSBUILD.EXE`. Use the `BUILD-NET48.CMD` file at the root to build all the projects in a row.

## See also...

### Other interesting open-source projects

#### pcsc-tools

On most Linux's distribution you can find the package pcsc-tools which provides some free software tools to send APDU commands to a card on Linux.

* [Official website](http://ludovic.rousseau.free.fr/softwares/pcsc-tools/)
* [Git repository](https://github.com/LudovicRousseau/pcsc-tools)

#### CardPeek

CardPeek can read cards' data, and is extensible with a scripting language (LUA). CardPeek runs on Windows, Linux, FreeBSD and Mac OS X.

* [Official website](http://pannetrat.com/Cardpeek)
* [Git repository](https://github.com/L1L1/cardpeek)

#### pcsc-sharp

pcsc-sharp is an alternative to `SpringCard.PCSC` library.

See [github.com/danm-de/pcsc-sharp](https://github.com/danm-de/pcsc-sharp).

## Legal disclaimer

THE SDK IS PROVIDED "AS IS" AND THE AUTHOR DISCLAIMS ALL WARRANTIES WITH REGARD TO THIS SOFTWARE INCLUDING ALL IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS. IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR ANY SPECIAL, DIRECT, INDIRECT, OR CONSEQUENTIAL DAMAGES OR ANY DAMAGES WHATSOEVER RESULTING FROM LOSS OF USE, DATA OR PROFITS, WHETHER IN AN ACTION OF CONTRACT, NEGLIGENCE OR OTHER TORTIOUS ACTION, ARISING OUT OF OR IN CONNECTION WITH THE USE OR PERFORMANCE OF THIS SOFTWARE.

## License

This software is part of SPRINGCARD SDKs

Redistribution and use in source (source code) and binary (object code) forms, with or without modification, are permitted provided that the following conditions are met :

1. Redistributed source code or object code shall be used only in conjunction with products (hardware devices) either manufactured, distributed or developed by SPRINGCARD,
2. Redistributed source code, either modified or un-modified, must retain the above copyright notice, this list of conditions and the disclaimer below,
3. Redistribution of any modified code must be clearly identified "Code derived from original SPRINGCARD copyrighted source code", with a description of the modification and the name of its author,
4. Redistributed object code must reproduce the above copyright notice, this list of conditions and the disclaimer below in the documentation and/or other materials provided with the distribution,
5. The name of SPRINGCARD may not be used to endorse or promote products derived from this software or in any other form without specific prior written permission from SPRINGCARD.

Please read to `LICENSE.TXT` for the complete license statement. Please always place a copy of the `LICENSE.TXT` file together with the redistributed source code or object code.

## How to contact us

Retrieve all our contact details on [www.springcard.com/contact](https://www.springcard.com/en/contact).
