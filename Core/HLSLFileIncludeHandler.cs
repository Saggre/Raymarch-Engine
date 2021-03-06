﻿// Modified on 20-Jun-2013 by Justin Stenning
// From original code by Alexandre Mutel.
// -------------------------------------------------------------------
// Original source in SharpDX.Toolkit.Graphics.FileIncludeHandler
// -------------------------------------------------------------------
// Copyright (c) 2010-2013 SharpDX - Alexandre Mutel
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using RaymarchEngine.Core.Primitives;
using RaymarchEngine.Core.Rendering;
using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.IO;
using Plane = RaymarchEngine.Core.Primitives.Plane;

namespace RaymarchEngine.Core
{
    /// <summary>
    /// Enables the usage of #include directive
    /// </summary>
    public class HLSLFileIncludeHandler : CallbackBase, Include
    {
        public readonly Stack<string> CurrentDirectory;
        public readonly List<string> IncludeDirectories;


        public HLSLFileIncludeHandler(string initialDirectory)
        {
            IncludeDirectories = new List<string>();
            CurrentDirectory = new Stack<string>();
            CurrentDirectory.Push(initialDirectory);
        }

        #region Include Members

        /// <summary>
        /// Creates and returns a file stream with constants such as baked array lengths
        /// </summary>
        /// <returns>Stream with HLSL code</returns>
        private Stream GetShaderConstantsStream()
        {
            string hlslString = $"static const int sphereCount = {RaymarchRenderer.PrimitiveCount<Sphere>()};" +
                                $"static const int boxCount = {RaymarchRenderer.PrimitiveCount<Box>()};" +
                                $"static const int planeCount = {RaymarchRenderer.PrimitiveCount<Plane>()};";

            Debug.WriteLine(hlslString);
            byte[] byteArray = Encoding.ASCII.GetBytes(hlslString);
            return new MemoryStream(byteArray);
        }

        public Stream Open(IncludeType type, string fileName, Stream parentStream)
        {
            Debug.WriteLine(fileName);

            // Include dynamic (:D) constants
            if (fileName.ToLower().Equals("raymarchengine"))
            {
                return GetShaderConstantsStream();
            }

            string currentDirectory = CurrentDirectory.Peek();
            if (currentDirectory == null)
            {
#if NETFX_CORE
                currentDirectory = Windows.ApplicationModel.Package.Current.InstalledLocation.Path;
#else
                currentDirectory = Environment.CurrentDirectory;
#endif
            }

            string filePath = fileName;

            if (!Path.IsPathRooted(filePath))
            {
                var directoryToSearch = new List<string> {currentDirectory};
                directoryToSearch.AddRange(IncludeDirectories);
                foreach (string dirPath in directoryToSearch)
                {
                    string selectedFile = Path.GetFullPath(Path.Combine(dirPath, fileName));
                    if (NativeFile.Exists(selectedFile))
                    {
                        filePath = selectedFile;
                        break;
                    }
                }
            }

            if (filePath == null || !NativeFile.Exists(filePath))
            {
                throw new FileNotFoundException(String.Format("Unable to find file [{0}]", filePath ?? fileName));
            }

            NativeFileStream fs = new NativeFileStream(filePath, NativeFileMode.Open, NativeFileAccess.Read);
            CurrentDirectory.Push(Path.GetDirectoryName(filePath));
            return fs;
        }

        public void Close(Stream stream)
        {
            stream.Dispose();
            if (stream.GetType() != typeof(MemoryStream))
            {
                CurrentDirectory.Pop();
            }
        }

        #endregion
    }
}