﻿// Created by Sakri Koskimies (Github: Saggre) on 29/09/2019

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using RaymarchEngine.Core;
using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using Buffer = SharpDX.Direct3D11.Buffer;
using Device = SharpDX.Direct3D11.Device;
using MapFlags = SharpDX.Direct3D11.MapFlags;

// TODO all
namespace RaymarchEngine.EMath
{
    public class ComputeShader
    {

        private Device d3dDevice;
        private DeviceContext d3dDeviceContext;
        private SharpDX.Direct3D11.ComputeShader computeShader;

        /// <summary>
        /// Dictionary key will be its index in the shader eg. register(u0);
        /// </summary>
        private Dictionary<int, BufferData> buffers;

        public ComputeShader(SharpDX.Direct3D11.ComputeShader computeShader)
        {
            Init();

            this.computeShader = computeShader;
        }

        /// <summary>
        /// Compiles files into shader byte-code and creates a shader from the shader files that exist
        /// </summary>
        /// <param name="folderPath"></param>
        /// <param name="fileName"></param>
        public ComputeShader(string folderPath, string fileName)
        {
            Init();

            ShaderFlags shaderFlags = ShaderFlags.Debug;
            SharpDX.Direct3D11.ComputeShader computeShader = null;

            // Handler for #include directive
            HLSLFileIncludeHandler includeHandler = new HLSLFileIncludeHandler(folderPath);

            string path = Path.Combine(folderPath, fileName);

            if (File.Exists(path))
            {
                CompilationResult byteCode = ShaderBytecode.CompileFromFile(path, "main", "cs_5_0", shaderFlags,
                    EffectFlags.None, null, includeHandler);
                computeShader = new SharpDX.Direct3D11.ComputeShader(d3dDevice, byteCode);
            }

            this.computeShader = computeShader;
        }

        /// <summary>
        /// 
        /// </summary>
        private void Init()
        {
            // Create separate device for compute shader
            d3dDevice = new Device(SharpDX.Direct3D.DriverType.Hardware, DeviceCreationFlags.SingleThreaded);
            d3dDeviceContext = d3dDevice.ImmediateContext;

            buffers = new Dictionary<int, BufferData>();
        }

        public void SetComputeBuffer(ComputeBuffer computeBuffer, int shaderBufferIndex)
        {
            BufferData bufferData = new BufferData(shaderBufferIndex, computeBuffer, typeof(ComputeBuffer));
            SetBufferData(shaderBufferIndex, bufferData);
        }

        /// <summary>
        /// Add a texture to the Compute Shader
        /// </summary>
        /// <param name="texture"></param>
        /// <param name="shaderBufferIndex"></param>
        public void SetTexture(Texture2D texture, int shaderBufferIndex)
        {
            BufferData bufferData = new BufferData(shaderBufferIndex, texture, typeof(Texture2D));
            SetBufferData(shaderBufferIndex, bufferData);
        }

        /// <summary>
        /// Generic method to add BufferData to the buffer
        /// </summary>
        /// <param name="shaderBufferIndex"></param>
        /// <param name="bufferData"></param>
        private void SetBufferData(int shaderBufferIndex, BufferData bufferData)
        {
            if (buffers.ContainsKey(shaderBufferIndex))
            {
                Debug.WriteLine("There is already a buffer with index " + shaderBufferIndex);
                return;
            }

            if (bufferData.GetInputDataType() == typeof(Texture2D))
            {
                UnorderedAccessView accessView = CreateUnorderedTextureAccessView((Texture2D)bufferData.GetInputData(), out var computeResource, out var stagingResource);
                bufferData.SetComputeResource(computeResource);
                bufferData.SetStagingResource(stagingResource);
                bufferData.SetUnorderedAccessView(accessView);
            }
            else if (bufferData.GetInputDataType() == typeof(ComputeBuffer))
            {
                UnorderedAccessView accessView = CreateUnorderedDataAccessView((ComputeBuffer)bufferData.GetInputData(), out var computeResource, out var stagingResource);
                bufferData.SetComputeResource(computeResource);
                bufferData.SetStagingResource(stagingResource);
                bufferData.SetUnorderedAccessView(accessView);
            }

            SendUnorderedAccessView(bufferData);
            buffers.Add(shaderBufferIndex, bufferData);
        }

        /// <summary>
        /// Create unordered access view for a texture
        /// </summary>
        /// <param name="inputData"></param>
        /// <param name="computeResource"></param>
        /// <param name="stagingResource"></param>
        /// <returns></returns>
        private UnorderedAccessView CreateUnorderedTextureAccessView(in Texture2D inputData, out Texture2D computeResource, out Texture2D stagingResource)
        {
            /*// Texture used by gpu
            computeResource = new Texture2D(d3dDevice, new Texture2DDescription()
            {
              BindFlags = BindFlags.UnorderedAccess | BindFlags.ShaderResource,
              Format = Format.R8G8B8A8_UNorm,
              Width = inputData.Width,
              Height = inputData.Height,
              OptionFlags = ResourceOptionFlags.None,
              MipLevels = 1,
              ArraySize = 1,
              SampleDescription = { Count = 1, Quality = 0 }
            });

            // Copy input texture
            float[] textureData = new float[inputData.Width * inputData.Height];
            //inputData.GetData<SharpDX.Color>(textureData);

            IntPtr dataPtr = GetDataPtr(inputData);
            Marshal.Copy(dataPtr, textureData, 0, textureData.Length * 4);

            // Add data pointer
            d3dDevice.ImmediateContext.UpdateSubresource(new DataBox(GetDataPtr(textureData)), computeResource);
            */

            computeResource = inputData;

            // Texture used to read pixels from
            stagingResource = new Texture2D(d3dDevice, new Texture2DDescription
            {
                CpuAccessFlags = CpuAccessFlags.Read,
                BindFlags = BindFlags.None,
                Format = Format.R8G8B8A8_UNorm,
                Width = inputData.Description.Width,
                Height = inputData.Description.Height,
                OptionFlags = ResourceOptionFlags.None,
                MipLevels = 1,
                ArraySize = 1,
                SampleDescription = { Count = 1, Quality = 0 },
                Usage = ResourceUsage.Staging
            });

            return new UnorderedAccessView(d3dDevice, computeResource, new UnorderedAccessViewDescription()
            {
                Format = Format.R8G8B8A8_UNorm,
                Dimension = UnorderedAccessViewDimension.Texture2D,
                Texture2D = { MipSlice = 0 }
            });
        }

        /// <summary>
        /// Add computeBufferData input into a buffer
        /// </summary>
        /// <param name="inputData"></param>
        /// <param name="computeResource"></param>
        /// <param name="stagingResource"></param>
        /// <returns></returns>
        private UnorderedAccessView CreateUnorderedDataAccessView(in ComputeBuffer inputData, out Buffer computeResource, out Buffer stagingResource)
        {
            //int size = Utilities.SizeOf<typeof(type)>();

            computeResource = new Buffer(d3dDevice, new BufferDescription()
            {
                BindFlags = BindFlags.UnorderedAccess | BindFlags.ShaderResource,
                Usage = ResourceUsage.Default,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.BufferStructured,
                StructureByteStride = inputData.Stride,
                SizeInBytes = inputData.Stride * inputData.Count
            });

            // Add data pointer
            d3dDeviceContext.UpdateSubresource(new DataBox(Core.Util.GetDataPtr(inputData.GetData())), computeResource);

            // Staging
            stagingResource = new Buffer(d3dDevice, new BufferDescription()
            {
                SizeInBytes = inputData.Stride * inputData.Count,
                BindFlags = BindFlags.None,
                CpuAccessFlags = CpuAccessFlags.Read | CpuAccessFlags.Write,
                Usage = ResourceUsage.Staging,
                OptionFlags = ResourceOptionFlags.None,
            });

            return new UnorderedAccessView(d3dDevice, computeResource, new UnorderedAccessViewDescription()
            {
                Buffer = new UnorderedAccessViewDescription.BufferResource() { FirstElement = 0, Flags = UnorderedAccessViewBufferFlags.None, ElementCount = inputData.Count },
                Format = Format.Unknown,
                Dimension = UnorderedAccessViewDimension.Buffer
            });

        }

        private void SendUnorderedAccessView(BufferData bufferData)
        {
            d3dDeviceContext.ComputeShader.SetUnorderedAccessView(bufferData.GetShaderBufferIndex(), bufferData.GetUnorderedAccessView());

        }

        private BufferData GetBufferDataById(int bufferDataId)
        {
            foreach (var keyValuePair in buffers)
            {
                if (keyValuePair.Key == bufferDataId)
                {
                    return keyValuePair.Value;
                }
            }

            return null;
        }

        public Texture2D GetTexture(int shaderBufferIndex)
        {
            BufferData tbd = GetBufferDataById(shaderBufferIndex);

            if (tbd.GetInputDataType() != typeof(Texture2D))
            {
                Debug.WriteLine("Buffer at index " + shaderBufferIndex + " is not a Texture2D");
                return null;
            }

            Texture2D stagingResource = tbd.GetStagingResource();

            // Copy resource
            d3dDeviceContext.CopyResource(tbd.GetComputeResource(), stagingResource);

            // Map to a data stream
            d3dDeviceContext.MapSubresource(stagingResource, 0, MapMode.Read, MapFlags.None, out var dataStream);


            /*
            // Read stream bytes
            byte[] bytes = new byte[1024 * 1024 * 4];
            for (var i = 0; i < bytes.Length; i++)
            {
              bytes[i] = (byte)dataStream.ReadByte();
            }
            dataStream.Close();
            */

            Texture2D t = new Texture2D(d3dDevice, new Texture2DDescription()
            {
                BindFlags = BindFlags.UnorderedAccess | BindFlags.ShaderResource,
                Format = Format.R8G8B8A8_UNorm,
                Width = 1024,
                Height = 1024,
                OptionFlags = ResourceOptionFlags.None,
                MipLevels = 1,
                ArraySize = 1,
                SampleDescription = { Count = 1, Quality = 0 }
            }, new DataRectangle(dataStream.DataPointer, 1024 * 4)); // TODO try to get dynamic tex directly

            return t;
        }

        /// <summary>
        /// Set current compute shader
        /// </summary>
        public void Begin()
        {
            d3dDeviceContext.ComputeShader.Set(computeShader);
        }

        public void Dispatch(int threadGroupCountX, int threadGroupCountY, int threadGroupCountZ)
        {
            d3dDevice.ImmediateContext.Dispatch(threadGroupCountX, threadGroupCountY, threadGroupCountZ);
        }

        public void End()
        {
            // Clean and jerk
            d3dDeviceContext.Flush();
            d3dDeviceContext.ComputeShader.SetUnorderedAccessView(0, null);
            foreach (var keyValuePair in buffers)
            {
                BufferData buffer = keyValuePair.Value;
                d3dDeviceContext.ComputeShader.SetUnorderedAccessView(buffer.GetShaderBufferIndex(), null);
            }
            // TODO remove compute shader?
            d3dDeviceContext.ComputeShader.Set(null);
        }


    }
}