﻿// Created by Sakri Koskimies (Github: Saggre) on 06/11/2019

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D11;

using Buffer11 = SharpDX.Direct3D11.Buffer;

namespace EconSim.Core.Rendering
{

    /// <summary>
    /// Execute Compute Shader outside Regular Graphics Device
    /// </summary>
    /// <typeparam name="T">Result Struct Type</typeparam>
    public class ComputeDevice<T> where T : struct
    {

        /// <summary>
        /// Device Pointer
        /// </summary>
        public Device Device
        {
            get { return _device; }
        }

        /// <summary>
        /// Device Context Pointer
        /// </summary>
        public DeviceContext DeviceContext
        {
            get { return _context; }
        }

        private Device _device;
        private DeviceContext _context;
        private Buffer11 _buffer;
        private UnorderedAccessView _accessView;
        private Buffer11 _resultBuffer;
        private ComputeShader _shader;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="folderPath"></param>
        /// <param name="fileName"></param>
        /// <param name="count">Max number of elements</param>
        public ComputeDevice(string folderPath, string fileName, int count)
        {
            ShaderFlags shaderFlags = ShaderFlags.Debug;

            _device = new Device(SharpDX.Direct3D.DriverType.Hardware, DeviceCreationFlags.SingleThreaded);
            _context = _device.ImmediateContext;

            _accessView = CreateUAV(count, out _buffer);
            _resultBuffer = CreateStaging(count);

            HLSLFileIncludeHandler includeHandler = new HLSLFileIncludeHandler(folderPath);

            string path = Path.Combine(folderPath, fileName);

            CompilationResult byteCode = ShaderBytecode.CompileFromFile(path, "main", "cs_5_0", shaderFlags,
                EffectFlags.None, null, includeHandler);
            _shader = new ComputeShader(Device, byteCode);
        }

        /// <summary>
        /// Prepare Device to compute shader
        /// </summary>
        public void Begin()
        {
            DeviceContext.ComputeShader.SetUnorderedAccessView(0, _accessView);
            DeviceContext.ComputeShader.Set(_shader);
        }

        /// <summary>
        /// End compute shader processing
        /// </summary>
        public void End()
        {
            DeviceContext.CopyResource(_buffer, _resultBuffer);
            DeviceContext.Flush();
            DeviceContext.ComputeShader.SetUnorderedAccessView(0, null);
            DeviceContext.ComputeShader.Set(null);
        }

        /// <summary>
        /// Execute Compute Shader code
        /// </summary>
        /// <param name="threadGroupCountX">Number of thread Group on X Axis</param>
        /// <param name="threadGroupCountY">Number of thread Group on Y Axis</param>
        /// <param name="threadGroupCountZ">Number of thread Group on Z Axis</param>
        public void Start(int threadGroupCountX, int threadGroupCountY, int threadGroupCountZ)
        {
            DeviceContext.Dispatch(threadGroupCountX, threadGroupCountY, threadGroupCountZ);
        }

        private Buffer11 CreateStaging(int count)
        {
            int size = SharpDX.Utilities.SizeOf<T>() * count;
            BufferDescription bufferDescription = new BufferDescription()
            {
                SizeInBytes = size,
                BindFlags = BindFlags.None,
                CpuAccessFlags = CpuAccessFlags.Read | CpuAccessFlags.Write,
                Usage = ResourceUsage.Staging,
                OptionFlags = ResourceOptionFlags.None,
            };

            return new Buffer11(Device, bufferDescription);
        }

        private UnorderedAccessView CreateUAV(int count, out Buffer11 buffer)
        {
            int size = SharpDX.Utilities.SizeOf<T>();
            BufferDescription bufferDescription = new BufferDescription()
            {
                BindFlags = BindFlags.UnorderedAccess | BindFlags.ShaderResource,
                Usage = ResourceUsage.Default,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.BufferStructured,
                StructureByteStride = size,
                SizeInBytes = size * count
            };

            buffer = new Buffer11(Device, bufferDescription);


            UnorderedAccessViewDescription uavDescription = new UnorderedAccessViewDescription()
            {
                Buffer = new UnorderedAccessViewDescription.BufferResource() { FirstElement = 0, Flags = UnorderedAccessViewBufferFlags.None, ElementCount = count },
                Format = SharpDX.DXGI.Format.Unknown,
                Dimension = UnorderedAccessViewDimension.Buffer
            };

            return new UnorderedAccessView(Device, buffer, uavDescription);

        }


        /// <summary>
        /// Return Executed Data
        /// </summary>
        /// <param name="count">Number of element to read</param>
        /// <returns>Result</returns>
        public T[] ReadData(int count)
        {
            DataStream stream;
            DataBox box = DeviceContext.MapSubresource(_resultBuffer, 0, MapMode.Read, MapFlags.None, out stream);
            T[] result = stream.ReadRange<T>(count);
            DeviceContext.UnmapSubresource(_buffer, 0);
            return result;

        }
    }
}