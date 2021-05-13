##Unit Testing

####Prerequisites

Before running the tests in Visual Studio, or by Command line, first we need to download the required test files.

To do so, we just need to run "1_DownloadTestFiles.cmd" once.  If we need to update the test files we can run it again.

After the test files have been successfully downloaded, we can either run the tests in Visual Studio, or run "2_RunCoreTests.cmd" and "3_RunToolkitTests.cmd"

####Overview

Tests are performed against Net471, NetCore3.1 and Net5.

The reason to test against the three frameworks is because there's slight differences between the platforms that makes some test to pass in one framework, and fail in the other.

Most notably, the differences found are:

- System.Text.Json
  - Dependency issues: NetCore3.1 and Net5 use different depedencies by default
  - Net471 implementation is not able to do floating point roundtrips.
- System.Numerics.Vectors Matrix4x4.CreatePerspective  fail in Net471 when trying to use infinite far planes.