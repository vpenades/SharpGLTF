using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NUnit.Framework;

[assembly: ResourcePathFormat("{SolutionDirectory}/tests/Assets")]
[assembly: AttachmentPathFormat("*/TestResults/?", true)]
