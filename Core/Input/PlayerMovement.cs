﻿// Created by Sakri Koskimies (Github: Saggre) on 24/10/2019

using System;
using System.Numerics;
using WindowsInput.Native;
using SharpDX.Windows;

namespace EconSim.Core.Input
{
    public class PlayerMovement : IUpdateable
    {
        private Vector3 movementInput;
        private Keyboard keyboard;

        public PlayerMovement()
        {
            keyboard = new Keyboard();
            StaticUpdater.Add(this);
        }

        /// <summary>
        /// Player movement input in X, Y, Z directions
        /// </summary>
        public Vector3 MovementInput => movementInput;

        public void Start(int startTime)
        {
        }

        public void Update(float deltaTime)
        {
            movementInput = Vector3.Zero;

            float playerSpeed = 1f;
            if (keyboard.IsKeyDown(VirtualKeyCode.LSHIFT))
                playerSpeed = 2f;

            if (keyboard.IsKeyDown(VirtualKeyCode.VK_D))
                movementInput -= Vector3.UnitX;
            if (keyboard.IsKeyDown(VirtualKeyCode.VK_A))
                movementInput += Vector3.UnitX;
            if (keyboard.IsKeyDown(VirtualKeyCode.VK_W))
                movementInput += Vector3.UnitZ;
            if (keyboard.IsKeyDown(VirtualKeyCode.VK_S))
                movementInput -= Vector3.UnitZ;
            if (keyboard.IsKeyDown(VirtualKeyCode.SPACE))
                movementInput -= Vector3.UnitY;
            if (keyboard.IsKeyDown(VirtualKeyCode.LCONTROL))
                movementInput += Vector3.UnitY;

        }

        public void End(int endTime)
        {
        }
    }
}