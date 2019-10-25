﻿// Created by Sakri Koskimies (Github: Saggre) on 02/10/2019

using System;
using System.Drawing;
using System.Numerics;
using System.Windows.Forms;
using SharpDX.Windows;

namespace EconSim.Core.Input
{

    public class Mouse : IUpdateable
    {
        /// <summary>
        /// FPS or menu mouse
        /// </summary>
        public enum MouseMode
        {
            Infinite = 0,
            Constrained = 1
        }

        private Vector2 position;
        private Vector2 deltaPosition;
        public MouseMode mouseMode;
        private readonly int screenX;
        private readonly int screenY;
        private readonly int screenHalfX;
        private readonly int screenHalfY;

        private RenderForm renderForm;

        private Point lastCursorPosition;

        public Mouse(RenderForm renderForm)
        {
            this.renderForm = renderForm;

            screenX = renderForm.Width;
            screenY = renderForm.Height;
            screenHalfX = screenX / 2;
            screenHalfY = screenY / 2;
            mouseMode = MouseMode.Infinite;

            SetCursorCenter();
            Update(0); // Do first update manually to prevent mouse jump at start

            StaticUpdater.Add(this);
        }

        public void HideCursor()
        {
            Cursor.Hide();
        }

        public void ShowCursor()
        {
            Cursor.Show();
        }

        public Vector2 DeltaPosition => deltaPosition;

        /// <summary>
        /// Current cursor position
        /// </summary>
        public Vector2 Position => position;

        /// <summary>
        /// Set cursor to a certain pixel on screen
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public void SetCursorPosition(int x, int y)
        {
            Cursor.Position = new Point(x, y);
        }

        /// <summary>
        /// Set cursor to a point on screen between [0,1]
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public void SetCursorPositionRelative(float x, float y)
        {
            Cursor.Position = new Point((int)(screenX * x), (int)(screenY * y));
        }

        /// <summary>
        /// Sets cursor to the center of the game viewport
        /// </summary>
        public void SetCursorCenter()
        {
            SetCursorPosition(renderForm.Left + screenHalfX, renderForm.Top + screenHalfY);
        }

        public void Start(int startTime)
        {

        }

        /// <summary>
        /// IUpdateable Update
        /// </summary>
        public void Update(float deltaTime)
        {
            Point cursorPosition = Cursor.Position;

            position.X = cursorPosition.X;
            position.Y = cursorPosition.Y;

            deltaPosition.X = cursorPosition.X - lastCursorPosition.X;
            deltaPosition.Y = cursorPosition.Y - lastCursorPosition.Y;

            // Center cursor
            if (mouseMode == MouseMode.Infinite)
            {
                SetCursorCenter();
            }

            lastCursorPosition = Cursor.Position;
        }

        public void End(int endTime)
        {

        }
    }
}