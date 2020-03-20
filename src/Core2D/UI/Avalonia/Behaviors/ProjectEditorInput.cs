﻿using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.PanAndZoom;
using Core2D.Editor;
using Core2D.Editor.Input;
using Core2D.UI.Avalonia.Utilities;

namespace Core2D.UI.Avalonia.Behaviors
{
    public class ProjectEditorInput
    {
        private readonly Control _control = null;
        private AvaloniaInputSource _inputSource = null;
        private ProjectEditorInputTarget _inputTarget = null;
        private InputProcessor _inputProcessor = null;

        public ProjectEditorInput(Control control)
        {
            _control = control;
            _control.GetObservable(Control.DataContextProperty).Subscribe(Changed);
        }

        public void InvalidateChild(double zoomX, double zoomY, double offsetX, double offsetY)
        {
            if (!(_control.DataContext is IProjectEditor projectEditor))
            {
                return;
            }

            var state = projectEditor.Renderers[0]?.State;
            if (state != null)
            {
                bool invalidateCache = state.ZoomX != zoomX || state.ZoomY != zoomY;
                state.ZoomX = zoomX;
                state.ZoomY = zoomY;
                state.PanX = offsetX;
                state.PanY = offsetY;
                if (invalidateCache)
                {
                    projectEditor.OnInvalidateCache(isZooming: true);
                }
            }
        }

        public void Changed(object context)
        {
            Detach();
            Attach();
        }

        public void Attach()
        {
            if (!(_control.DataContext is IProjectEditor projectEditor))
            {
                return;
            }

            var containerControlData = _control.Find<Control>("containerControlData");
            var containerControlTemplate = _control.Find<Control>("containerControlTemplate");
            var containerControlEditor = _control.Find<Control>("containerControlEditor");
            var zoomBorder = _control.Find<ZoomBorder>("zoomBorder");

            if (projectEditor.CanvasPlatform is IEditorCanvasPlatform canvasPlatform)
            {
                canvasPlatform.Invalidate = () =>
                {
                    containerControlData?.InvalidateVisual();
                    containerControlTemplate?.InvalidateVisual();
                    containerControlEditor?.InvalidateVisual();
                };
                canvasPlatform.ResetZoom = () => zoomBorder?.Reset();
                canvasPlatform.AutoFitZoom = () => zoomBorder?.AutoFit();
                canvasPlatform.Zoom = zoomBorder;
            }

            if (zoomBorder != null)
            {
                zoomBorder.InvalidatedChild = InvalidateChild;
            }

            _inputSource = new AvaloniaInputSource(zoomBorder, containerControlEditor, p => p);
            _inputTarget = new ProjectEditorInputTarget(projectEditor);
            _inputProcessor = new InputProcessor();
            _inputProcessor.Connect(_inputSource, _inputTarget);
        }

        public void Detach()
        {
            if (!(_control.DataContext is IProjectEditor projectEditor))
            {
                return;
            }

            var zoomBorder = _control.Find<ZoomBorder>("zoomBorder");

            if (projectEditor.CanvasPlatform is IEditorCanvasPlatform canvasPlatform)
            {
                canvasPlatform.Invalidate = null;
                canvasPlatform.ResetZoom = null;
                canvasPlatform.AutoFitZoom = null;
                canvasPlatform.Zoom = null;
            }

            if (zoomBorder != null)
            {
                zoomBorder.InvalidatedChild = null;
            }

            _inputProcessor?.Dispose();
            _inputProcessor = null;
            _inputTarget = null;
            _inputSource = null;
        }
    }
}
