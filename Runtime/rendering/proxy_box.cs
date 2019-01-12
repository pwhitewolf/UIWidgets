﻿using System.Collections.Generic;
using Unity.UIWidgets.animation;
using Unity.UIWidgets.foundation;
using Unity.UIWidgets.gestures;
using Unity.UIWidgets.painting;
using Unity.UIWidgets.ui;

namespace Unity.UIWidgets.rendering {
    public class RenderProxyBox : RenderProxyBoxMixinRenderObjectWithChildMixinRenderBox<RenderBox> {
        public RenderProxyBox(RenderBox child = null) {
            this.child = child;
        }
    }

    public enum HitTestBehavior {
        deferToChild,
        opaque,
        translucent,
    }

    public abstract class RenderProxyBoxWithHitTestBehavior : RenderProxyBox {
        protected RenderProxyBoxWithHitTestBehavior(
            HitTestBehavior behavior = HitTestBehavior.deferToChild,
            RenderBox child = null
        ) : base(child) {
            this.behavior = behavior;
        }

        public HitTestBehavior behavior;

        public override bool hitTest(HitTestResult result, Offset position = null) {
            bool hitTarget = false;
            if (this.size.contains(position)) {
                hitTarget = this.hitTestChildren(result, position: position) || this.hitTestSelf(position);
                if (hitTarget || this.behavior == HitTestBehavior.translucent) {
                    result.add(new BoxHitTestEntry(this, position));
                }
            }

            return hitTarget;
        }

        protected override bool hitTestSelf(Offset position) {
            return this.behavior == HitTestBehavior.opaque;
        }

        public override void debugFillProperties(DiagnosticPropertiesBuilder properties) {
            base.debugFillProperties(properties);
            properties.add(new EnumProperty<HitTestBehavior>(
                "behavior", this.behavior, defaultValue: Diagnostics.kNullDefaultValue));
        }
    }

    public class RenderConstrainedBox : RenderProxyBox {
        public RenderConstrainedBox(
            RenderBox child = null,
            BoxConstraints additionalConstraints = null) : base(child) {
            D.assert(additionalConstraints != null);
            D.assert(additionalConstraints.debugAssertIsValid());

            this._additionalConstraints = additionalConstraints;
        }

        public BoxConstraints additionalConstraints {
            get { return this._additionalConstraints; }
            set {
                D.assert(value != null);
                D.assert(value.debugAssertIsValid());

                if (this._additionalConstraints == value) {
                    return;
                }

                this._additionalConstraints = value;
                this.markNeedsLayout();
            }
        }

        BoxConstraints _additionalConstraints;

        protected override double computeMinIntrinsicWidth(double height) {
            if (this._additionalConstraints.hasBoundedWidth && this._additionalConstraints.hasTightWidth) {
                return this._additionalConstraints.minWidth;
            }

            double width = base.computeMinIntrinsicWidth(height);
            D.assert(width.isFinite());

            if (!this._additionalConstraints.hasInfiniteWidth) {
                return this._additionalConstraints.constrainWidth(width);
            }

            return width;
        }

        protected override double computeMaxIntrinsicWidth(double height) {
            if (this._additionalConstraints.hasBoundedWidth && this._additionalConstraints.hasTightWidth) {
                return this._additionalConstraints.minWidth;
            }

            double width = base.computeMaxIntrinsicWidth(height);
            D.assert(width.isFinite());

            if (!this._additionalConstraints.hasInfiniteWidth) {
                return this._additionalConstraints.constrainWidth(width);
            }

            return width;
        }

        protected override double computeMinIntrinsicHeight(double width) {
            if (this._additionalConstraints.hasBoundedHeight && this._additionalConstraints.hasTightHeight) {
                return this._additionalConstraints.minHeight;
            }

            double height = base.computeMinIntrinsicHeight(width);
            D.assert(height.isFinite());

            if (!this._additionalConstraints.hasInfiniteHeight) {
                return this._additionalConstraints.constrainHeight(height);
            }

            return height;
        }

        protected override double computeMaxIntrinsicHeight(double width) {
            if (this._additionalConstraints.hasBoundedHeight && this._additionalConstraints.hasTightHeight) {
                return this._additionalConstraints.minHeight;
            }

            double height = base.computeMaxIntrinsicHeight(width);
            D.assert(height.isFinite());

            if (!this._additionalConstraints.hasInfiniteHeight) {
                return this._additionalConstraints.constrainHeight(height);
            }

            return height;
        }

        protected override void performLayout() {
            if (this.child != null) {
                this.child.layout(this._additionalConstraints.enforce(this.constraints), parentUsesSize: true);
                this.size = this.child.size;
            } else {
                this.size = this._additionalConstraints.enforce(this.constraints).constrain(Size.zero);
            }
        }

        protected override void debugPaintSize(PaintingContext context, Offset offset) {
            base.debugPaintSize(context, offset);
            D.assert(() => {
                if (this.child == null || this.child.size.isEmpty) {
                    var paint = new Paint {
                        color = new Color(0x90909090)
                    };
//                    context.canvas.drawRect(offset & this.size, BorderWidth.zero, BorderRadius.zero, paint);
                }

                return true;
            });
        }

        public override void debugFillProperties(DiagnosticPropertiesBuilder properties) {
            base.debugFillProperties(properties);
            properties.add(
                new DiagnosticsProperty<BoxConstraints>("additionalConstraints", this.additionalConstraints));
        }
    }

    public class RenderLimitedBox : RenderProxyBox {
        public RenderLimitedBox(
            RenderBox child = null,
            double maxWidth = double.PositiveInfinity,
            double maxHeight = double.PositiveInfinity
        ) : base(child) {
            D.assert(maxWidth >= 0.0);
            D.assert(maxHeight >= 0.0);

            this._maxWidth = maxWidth;
            this._maxHeight = maxHeight;
        }

        public double maxWidth {
            get { return this._maxWidth; }
            set {
                D.assert(value >= 0.0);
                if (this._maxWidth == value) {
                    return;
                }

                this._maxWidth = value;
                this.markNeedsLayout();
            }
        }

        double _maxWidth;

        public double maxHeight {
            get { return this._maxHeight; }
            set {
                D.assert(value >= 0.0);
                if (this._maxHeight == value) {
                    return;
                }

                this._maxHeight = value;
                this.markNeedsLayout();
            }
        }

        double _maxHeight;

        BoxConstraints _limitConstraints(BoxConstraints constraints) {
            return new BoxConstraints(
                minWidth: constraints.minWidth,
                maxWidth: constraints.hasBoundedWidth
                    ? constraints.maxWidth
                    : constraints.constrainWidth(this.maxWidth),
                minHeight: constraints.minHeight,
                maxHeight: constraints.hasBoundedHeight
                    ? constraints.maxHeight
                    : constraints.constrainHeight(this.maxHeight)
            );
        }

        protected override void performLayout() {
            if (this.child != null) {
                this.child.layout(this._limitConstraints(this.constraints), parentUsesSize: true);
                this.size = this.constraints.constrain(this.child.size);
            } else {
                this.size = this._limitConstraints(this.constraints).constrain(Size.zero);
            }
        }

        public override void debugFillProperties(DiagnosticPropertiesBuilder properties) {
            base.debugFillProperties(properties);
            properties.add(new DoubleProperty("maxWidth", this.maxWidth, defaultValue: double.PositiveInfinity));
            properties.add(new DoubleProperty("maxHeight", this.maxHeight, defaultValue: double.PositiveInfinity));
        }
    }

    public class RenderAspectRatio : RenderProxyBox {
        public RenderAspectRatio(double aspectRatio, RenderBox child = null) : base(child) {
            this._aspectRatio = aspectRatio;
        }

        public double aspectRatio {
            get { return this._aspectRatio; }
            set {
                if (this._aspectRatio == value) {
                    return;
                }

                this._aspectRatio = value;
                this.markNeedsLayout();
            }
        }

        double _aspectRatio;


        protected override double computeMinIntrinsicWidth(double height) {
            if (!double.IsInfinity(height)) {
                return height * this._aspectRatio;
            }
            if (this.child != null) {
                return this.child.getMinIntrinsicWidth(height);
            }
            return 0.0;
        }

        protected override double computeMaxIntrinsicWidth(double height) {
            if (!double.IsInfinity(height)) {
                return height * this._aspectRatio;
            }
            if (this.child != null) {
                return this.child.getMaxIntrinsicWidth(height);
            }

            return 0.0;
        }

        protected override double computeMinIntrinsicHeight(double width) {
            if (!double.IsInfinity(width)) {
                return width / this._aspectRatio;
            }
            if (this.child != null) {
                return this.child.getMinIntrinsicHeight(width);
            }

            return 0.0;
        }

        protected override double computeMaxIntrinsicHeight(double width) {
            if (!double.IsInfinity(width)) {
                return width / this._aspectRatio;
            }
            if (this.child != null) {
                return this.child.getMaxIntrinsicHeight(width);
            }

            return 0.0;
        }

        Size _applyAspectRatio(BoxConstraints constraints) {
            D.assert(constraints.debugAssertIsValid());
            if (constraints.isTight) {
                return constraints.smallest;
            }

            double width = constraints.maxWidth;
            double height = width / this._aspectRatio;

            if (width > constraints.maxWidth) {
                width = constraints.maxWidth;
                height = width / this._aspectRatio;
            }

            if (height > constraints.maxHeight) {
                height = constraints.maxHeight;
                width = height * this._aspectRatio;
            }

            if (width < constraints.minWidth) {
                width = constraints.minWidth;
                height = width / this._aspectRatio;
            }

            if (height < constraints.minHeight) {
                height = constraints.minHeight;
                width = height * this._aspectRatio;
            }

            return constraints.constrain(new Size(width, height));
        }

        protected override void performLayout() {
            this.size = this._applyAspectRatio(this.constraints);
            if (this.child != null) {
                this.child.layout(BoxConstraints.tight(this.size));
            }
        }

        public override void debugFillProperties(DiagnosticPropertiesBuilder properties) {
            base.debugFillProperties(properties);
            properties.add(new DoubleProperty("aspectRatio", this.aspectRatio));
        }
    }

    public class RenderOpacity : RenderProxyBox {
        public RenderOpacity(double opacity = 1.0, RenderBox child = null) : base(child) {
            D.assert(opacity >= 0.0 && opacity <= 1.0);
            this._opacity = opacity;
            this._alpha = _getAlphaFromOpacity(opacity);
        }

        protected override bool alwaysNeedsCompositing {
            get { return this.child != null && (this._alpha != 0 && this._alpha != 255); }
        }

        int _alpha;

        internal static int _getAlphaFromOpacity(double opacity) {
            return (opacity * 255).round();
        }

        double _opacity;
        public double opacity {
            get { return this._opacity; }
            set {
                D.assert(value >= 0.0 && value <= 1.0);
                if (this._opacity == value) {
                    return;
                }

                bool didNeedCompositing = this.alwaysNeedsCompositing;

                this._opacity = value;
                this._alpha = _getAlphaFromOpacity(this._opacity);

                if (didNeedCompositing != this.alwaysNeedsCompositing) {
                    this.markNeedsCompositingBitsUpdate();
                }

                this.markNeedsPaint();
            }
        }

        public override void paint(PaintingContext context, Offset offset) {
            if (this.child != null) {
                if (this._alpha == 0) {
                    return;
                }
            }

            if (this._alpha == 255) {
                context.paintChild(this.child, offset);
                return;
            }

            D.assert(this.needsCompositing);
            context.pushOpacity(offset, this._alpha, base.paint);
        }

        public override void debugFillProperties(DiagnosticPropertiesBuilder properties) {
            base.debugFillProperties(properties);
            properties.add(new DoubleProperty("opacity", this.opacity));
        }
    }

    public class RenderAnimatedOpacity : RenderProxyBox {

        public RenderAnimatedOpacity(
            Animation<double> opacity = null,
                RenderBox child = null
        ) : base(child) {
            D.assert(opacity != null);
            this.opacity = opacity;
        }


        int _alpha;
        
        protected override bool alwaysNeedsCompositing => this.child != null && this._currentlyNeedsCompositing;
        bool _currentlyNeedsCompositing;
        
        Animation<double> _opacity;
        public Animation<double>  opacity {
            get => this._opacity;
            set {
                D.assert(value != null);
                if (this._opacity == value)
                    return;
                if (this.attached && this._opacity != null)
                    this._opacity.removeListener(this._updateOpacity);
                this._opacity = value;
                if (this.attached)
                    this._opacity.addListener(this._updateOpacity);
                this._updateOpacity();
            }
        }
        
        
        public override void attach(object owner) {
            base.attach(owner);
            this._opacity.addListener(this._updateOpacity);
            this._updateOpacity(); // in case it changed while we weren't listening
        }

        public override void detach() {
            this._opacity.removeListener(this._updateOpacity);
            base.detach();
        }

        public void _updateOpacity() {
            var oldAlpha = this._alpha;
            this._alpha = RenderOpacity._getAlphaFromOpacity(this._opacity.value.clamp(0.0, 1.0));
            if (oldAlpha != this._alpha) {
                bool didNeedCompositing = this._currentlyNeedsCompositing;
                this._currentlyNeedsCompositing = this._alpha > 0 &&this. _alpha < 255;
                if (this.child != null && didNeedCompositing != this._currentlyNeedsCompositing)
                    this.markNeedsCompositingBitsUpdate();
                this.markNeedsPaint();
            }
        }
        
        public override void paint(PaintingContext context, Offset offset) {
            if (this.child != null) {
                if (this._alpha == 0)
                    return;
                if (this._alpha == 255) {
                    context.paintChild(this.child, offset);
                    return;
                }
                D.assert(this.needsCompositing);
                context.pushOpacity(offset, this._alpha, base.paint);
            }
        }
        
        public override void debugFillProperties(DiagnosticPropertiesBuilder properties) {
            base.debugFillProperties(properties);
            properties.add(new DiagnosticsProperty<Animation<double>>("opacity", this.opacity));
         }
    }

    public enum DecorationPosition {
        background,
        foreground,
    }

    public class RenderDecoratedBox : RenderProxyBox {
        public RenderDecoratedBox(
            Decoration decoration = null,
            DecorationPosition position = DecorationPosition.background,
            ImageConfiguration configuration = null,
            RenderBox child = null
        ) : base(child) {
            D.assert(decoration != null);
            this._decoration = decoration;
            this._position = position;
            this._configuration = configuration ?? ImageConfiguration.empty;
        }

        BoxPainter _painter;

        public Decoration decoration {
            get { return this._decoration; }
            set {
                D.assert(value != null);
                if (value == this._decoration) {
                    return;
                }

                if (this._painter != null) {
                    this._painter.dispose();
                    this._painter = null;
                }

                this._decoration = value;
                this.markNeedsPaint();
            }
        }

        Decoration _decoration;

        public DecorationPosition position {
            get { return this._position; }
            set {
                if (value == this._position) {
                    return;
                }

                this._position = value;
                this.markNeedsPaint();
            }
        }

        DecorationPosition _position;

        public ImageConfiguration configuration {
            get { return this._configuration; }
            set {
                D.assert(value != null);
                if (value == this._configuration) {
                    return;
                }

                this._configuration = value;
                this.markNeedsPaint();
            }
        }

        ImageConfiguration _configuration;

        public override void detach() {
            if (this._painter != null) {
                this._painter.dispose();
                this._painter = null;
            }

            base.detach();
            this.markNeedsPaint();
        }

        protected override bool hitTestSelf(Offset position) {
            return this._decoration.hitTest(this.size, position);
        }

        public override void paint(PaintingContext context, Offset offset) {
            this._painter = this._painter ?? this._decoration.createBoxPainter(this.markNeedsPaint);
            var filledConfiguration = this.configuration.copyWith(size: this.size);

            if (this.position == DecorationPosition.background) {
                int debugSaveCount = 0;
                D.assert(() => {
                    debugSaveCount = context.canvas.getSaveCount();
                    return true;
                });

                this._painter.paint(context.canvas, offset, filledConfiguration);

                D.assert(() => {
                    if (debugSaveCount != context.canvas.getSaveCount()) {
                        throw new UIWidgetsError(
                            this._decoration.GetType() + " painter had mismatching save and restore calls.\n" +
                            "Before painting the decoration, the canvas save count was $debugSaveCount. " +
                            "After painting it, the canvas save count was " + context.canvas.getSaveCount() + ". " +
                            "Every call to save() or saveLayer() must be matched by a call to restore().\n" +
                            "The decoration was:\n" +
                            "  " + this.decoration + "\n" +
                            "The painter was:\n" +
                            "  " + this._painter
                        );
                    }

                    return true;
                });

                if (this.decoration.isComplex) {
                    context.setIsComplexHint();
                }
            }

            base.paint(context, offset);

            if (this.position == DecorationPosition.foreground) {
                this._painter.paint(context.canvas, offset, filledConfiguration);
                if (this.decoration.isComplex) {
                    context.setIsComplexHint();
                }
            }
        }

        public override void debugFillProperties(DiagnosticPropertiesBuilder properties) {
            base.debugFillProperties(properties);
            properties.add(this._decoration.toDiagnosticsNode(name: "decoration"));
            properties.add(new DiagnosticsProperty<ImageConfiguration>("configuration", this.configuration));
        }
    }

    public class RenderTransform : RenderProxyBox {
        public RenderTransform(
            Matrix3 transform,
            Offset origin = null,
            Alignment alignment = null,
            bool transformHitTests = true,
            RenderBox child = null
        ) : base(child) {
            this.transform = transform;
            this.origin = origin;
            this.alignment = alignment;
            this.transformHitTests = transformHitTests;
        }

        public Offset origin {
            get { return this._origin; }
            set {
                if (this._origin == value) {
                    return;
                }

                this._origin = value;
                this.markNeedsPaint();
            }
        }

        Offset _origin;

        public Alignment alignment {
            get { return this._alignment; }
            set {
                if (this._alignment == value) {
                    return;
                }

                this._alignment = value;
                this.markNeedsPaint();
            }
        }

        Alignment _alignment;

        public bool transformHitTests;

        public Matrix3 transform {
            set {
                if (this._transform == value) {
                    return;
                }

                this._transform = value;
                this.markNeedsPaint();
            }
        }

        Matrix3 _transform;

        public void setIdentity() {
            this._transform = Matrix3.I();
            this.markNeedsPaint();
        }

        public void rotateX(double degrees) {
            //2D, do nothing
        }

        public void rotateY(double degrees) {
            //2D, do nothing
        }

        public void rotateZ(double degrees) {
            this._transform.preRotate((float)degrees);
            this.markNeedsPaint();
        }

        public void translate(double x, double y = 0.0, double z = 0.0) {
            this._transform.preTranslate((float) x, (float) y);
            this.markNeedsPaint();
        }

        public void scale(double x, double y, double z) {
            this._transform.preScale((float) x, (float) y);
            this.markNeedsPaint();
        }

        Matrix3 _effectiveTransform {
            get {
                Alignment resolvedAlignment = this.alignment;
                if (this._origin == null && resolvedAlignment == null) {
                    return this._transform;
                }

                var result = Matrix3.I();
                if (this._origin != null) {
                    result.preTranslate((float) this._origin.dx, (float) this._origin.dy);
                }

                Offset translation = null;
                if (resolvedAlignment != null) {
                    translation = resolvedAlignment.alongSize(this.size);                    
                    result.preTranslate((float) translation.dx, (float) translation.dy);
                }

                result.preConcat(this._transform);

                if (resolvedAlignment != null) {
                    result.preTranslate((float) -translation.dx, (float) -translation.dy);
                }

                if (this._origin != null) {
                    result.preTranslate((float) -this._origin.dx, (float) -this._origin.dy);
                }

                return result;
            }
        }

        public override bool hitTest(HitTestResult result, Offset position = null) {
            return this.hitTestChildren(result, position: position);
        }

        protected override bool hitTestChildren(HitTestResult result, Offset position = null) {
            if (this.transformHitTests) {
                var transform = this._effectiveTransform;
                var inverse = Matrix3.I();
                var invertible = transform.invert(inverse);

                if (!invertible) {
                    return false;
                }

                position = inverse.mapPoint(position);
            }

            return base.hitTestChildren(result, position: position);
        }

        public override void paint(PaintingContext context, Offset offset) {
            if (this.child != null) {
                var transform = this._effectiveTransform;
                Offset childOffset = transform.getAsTranslation();

                if (childOffset == null) {
                    context.pushTransform(this.needsCompositing, offset, transform, base.paint);
                } else {
                    base.paint(context, offset + childOffset);
                }
            }
        }

        public override void applyPaintTransform(RenderObject child, Matrix3 transform) {
            transform.preConcat(this._effectiveTransform);
        }

        public override void debugFillProperties(DiagnosticPropertiesBuilder properties) {
            base.debugFillProperties(properties);
            properties.add(new DiagnosticsProperty<Matrix3>("transform matrix", this._transform));
            properties.add(new DiagnosticsProperty<Offset>("origin", this.origin));
            properties.add(new DiagnosticsProperty<Alignment>("alignment", this.alignment));
            properties.add(new DiagnosticsProperty<bool>("transformHitTests", this.transformHitTests));
        }
    }

    public class RenderFractionalTranslation : RenderProxyBox {

        public RenderFractionalTranslation(
            Offset translation = null,
            bool transformHitTests = true,
            RenderBox child = null
        ) : base(child: child) {
            D.assert(translation != null);
            this._translation = translation;
            this.transformHitTests = transformHitTests;
        }

        public Offset translation {
            get => this._translation;

            set {
                D.assert(value != null);
                if (this._translation == value)
                    return;
                this._translation = value;
                this.markNeedsPaint();
            }
        }
        Offset _translation;

        public override bool hitTest(HitTestResult result, Offset position) {
            return this.hitTestChildren(result, position: position);
        }
        
        public bool transformHitTests;
        
        protected override bool hitTestChildren(HitTestResult result, Offset position) {
            D.assert(!this.debugNeedsLayout);
            if (this.transformHitTests) {
                position = new Offset(
                    position.dx - this.translation.dx * this.size.width,
                    position.dy - this.translation.dy * this.size.height
                );
            }
            return base.hitTestChildren(result, position: position);
        }
        
        public override void paint(PaintingContext context, Offset offset) {
            D.assert(!this.debugNeedsLayout);
            if (this.child != null) {
                base.paint(context, new Offset(
                    offset.dx + this.translation.dx * this.size.width,
                    offset.dy + this.translation.dy * this.size.height
                ));
            }
        }
        
       public override void applyPaintTransform(RenderObject child, Matrix3 transform) {
            transform.preTranslate((float)(this.translation.dx * this.size.width), 
                            (float)(this.translation.dy * this.size.height));

       }
       
       public override void debugFillProperties(DiagnosticPropertiesBuilder properties) {
           base.debugFillProperties(properties);
           properties.add(new DiagnosticsProperty<Offset>("translation", this.translation));
           properties.add(new DiagnosticsProperty<bool>("transformHitTests", this.transformHitTests));
       }
    }

    public delegate void PointerDownEventListener(PointerDownEvent evt);

    public delegate void PointerMoveEventListener(PointerMoveEvent evt);

    public delegate void PointerUpEventListener(PointerUpEvent evt);

    public delegate void PointerCancelEventListener(PointerCancelEvent evt);

    public delegate void PointerHoverEventListener(PointerHoverEvent evt);

    public delegate void PointerEnterEventListener(PointerEnterEvent evt);

    public delegate void PointerLeaveEventListener(PointerLeaveEvent evt);

    public class RenderPointerListener : RenderProxyBoxWithHitTestBehavior {
        public RenderPointerListener(
            PointerDownEventListener onPointerDown = null,
            PointerMoveEventListener onPointerMove = null,
            PointerUpEventListener onPointerUp = null,
            PointerCancelEventListener onPointerCancel = null,
            PointerHoverEventListener onPointerHover = null,
            PointerLeaveEventListener onPointerLeave = null,
            PointerEnterEventListener onPointerEnter = null,
            HitTestBehavior behavior = HitTestBehavior.deferToChild,
            RenderBox child = null
        ) : base(behavior: behavior, child: child) {
            this.onPointerDown = onPointerDown;
            this.onPointerMove = onPointerMove;
            this.onPointerUp = onPointerUp;
            this.onPointerCancel = onPointerCancel;
            this.onPointerHover = onPointerHover;
            this.onPointerLeave = onPointerLeave;
            this.onPointerEnter = onPointerEnter;
        }

        public PointerDownEventListener onPointerDown;

        public PointerMoveEventListener onPointerMove;

        public PointerUpEventListener onPointerUp;

        public PointerCancelEventListener onPointerCancel;

        public PointerHoverEventListener onPointerHover;

        public PointerLeaveEventListener onPointerLeave;

        public PointerEnterEventListener onPointerEnter;

        protected override void performResize() {
            this.size = this.constraints.biggest;
        }

        public override void handleEvent(PointerEvent evt, HitTestEntry entry) {
            D.assert(this.debugHandleEvent(evt, entry));

            if (this.onPointerDown != null && evt is PointerDownEvent) {
                this.onPointerDown((PointerDownEvent) evt);
                return;
            }

            if (this.onPointerMove != null && evt is PointerMoveEvent) {
                this.onPointerMove((PointerMoveEvent) evt);
                return;
            }

            if (this.onPointerUp != null && evt is PointerUpEvent) {
                this.onPointerUp((PointerUpEvent) evt);
                return;
            }

            if (this.onPointerCancel != null && evt is PointerCancelEvent) {
                this.onPointerCancel((PointerCancelEvent) evt);
                return;
            }

            if (this.onPointerHover != null && evt is PointerHoverEvent) {
                this.onPointerHover((PointerHoverEvent) evt);
                return;
            }

            if (this.onPointerLeave != null && evt is PointerLeaveEvent) {
                this.onPointerLeave((PointerLeaveEvent) evt);
                return;
            }

            if (this.onPointerEnter != null && evt is PointerEnterEvent) {
                this.onPointerEnter((PointerEnterEvent) evt);
                return;
            }
        }

        public override void debugFillProperties(DiagnosticPropertiesBuilder properties) {
            base.debugFillProperties(properties);
            var listeners = new List<string>();
            if (this.onPointerDown != null) {
                listeners.Add("down");
            }
            if (this.onPointerMove != null) {
                listeners.Add("move");
            }
            if (this.onPointerUp != null) {
                listeners.Add("up");
            }
            if (this.onPointerCancel != null) {
                listeners.Add("cancel");
            }
            if (this.onPointerHover != null) {
                listeners.Add("hover");
            }
            if (this.onPointerEnter != null) {
                listeners.Add("enter");
            }
            if (this.onPointerLeave != null) {
                listeners.Add("leave");
            }
            if (listeners.isEmpty()) {
                listeners.Add("<none>");
            }

            properties.add(new EnumerableProperty<string>("listeners", listeners));
        }
    }

    public class RenderRepaintBoundary : RenderProxyBox {
        public RenderRepaintBoundary(
            RenderBox child = null
        ) : base(child) {
        }

        public override bool isRepaintBoundary {
            get { return true; }
        }

        public int debugSymmetricPaintCount {
            get { return this._debugSymmetricPaintCount; }
        }

        int _debugSymmetricPaintCount = 0;

        public int debugAsymmetricPaintCount {
            get { return this._debugAsymmetricPaintCount; }
        }

        int _debugAsymmetricPaintCount = 0;

        public void debugResetMetrics() {
            D.assert(() => {
                this._debugSymmetricPaintCount = 0;
                this._debugAsymmetricPaintCount = 0;
                return true;
            });
        }

        public override void debugRegisterRepaintBoundaryPaint(bool includedParent = true, bool includedChild = false) {
            D.assert(() => {
                if (includedParent && includedChild) {
                    this._debugSymmetricPaintCount += 1;
                } else {
                    this._debugAsymmetricPaintCount += 1;
                }

                return true;
            });
        }

        public override void debugFillProperties(DiagnosticPropertiesBuilder properties) {
            base.debugFillProperties(properties);
            bool inReleaseMode = true;
            D.assert(() => {
                inReleaseMode = false;
                if (this.debugSymmetricPaintCount + this.debugAsymmetricPaintCount == 0) {
                    properties.add(new MessageProperty("usefulness ratio", "no metrics collected yet (never painted)"));
                } else {
                    double fraction = (double) this.debugAsymmetricPaintCount /
                                      (this.debugSymmetricPaintCount + this.debugAsymmetricPaintCount);

                    string diagnosis;
                    if (this.debugSymmetricPaintCount + this.debugAsymmetricPaintCount < 5) {
                        diagnosis = "insufficient data to draw conclusion (less than five repaints)";
                    } else if (fraction > 0.9) {
                        diagnosis = "this is an outstandingly useful repaint boundary and should definitely be kept";
                    } else if (fraction > 0.5) {
                        diagnosis = "this is a useful repaint boundary and should be kept";
                    } else if (fraction > 0.30) {
                        diagnosis =
                            "this repaint boundary is probably useful, but maybe it would be more useful in tandem with adding more repaint boundaries elsewhere";
                    } else if (fraction > 0.1) {
                        diagnosis = "this repaint boundary does sometimes show value, though currently not that often";
                    } else if (this.debugAsymmetricPaintCount == 0) {
                        diagnosis = "this repaint boundary is astoundingly ineffectual and should be removed";
                    } else {
                        diagnosis = "this repaint boundary is not very effective and should probably be removed";
                    }

                    properties.add(new PercentProperty("metrics", fraction, unit: "useful",
                        tooltip: this.debugSymmetricPaintCount + " bad vs " + this.debugAsymmetricPaintCount +
                                 " good"));
                    properties.add(new MessageProperty("diagnosis", diagnosis));
                }

                return true;
            });
            if (inReleaseMode) {
                properties.add(DiagnosticsNode.message("(run in checked mode to collect repaint boundary statistics)"));
            }
        }
    }

    public class RenderIgnorePointer : RenderProxyBox {
        public RenderIgnorePointer(
            RenderBox child = null,
            bool ignoring = true
        ) : base(child) {
            this._ignoring = ignoring;
        }

        public bool ignoring {
            get { return this._ignoring; }
            set {
                if (value == this._ignoring) {
                    return;
                }

                this._ignoring = value;
            }
        }

        bool _ignoring;

        public override bool hitTest(HitTestResult result, Offset position = null) {
            return this.ignoring ? false : base.hitTest(result, position: position);
        }

        public override void debugFillProperties(DiagnosticPropertiesBuilder properties) {
            base.debugFillProperties(properties);
            properties.add(new DiagnosticsProperty<bool>("ignoring", this.ignoring));
        }
    }

    public class RenderOffstage : RenderProxyBox {
        public RenderOffstage(bool offstage = true,
            RenderBox child = null): base(child) {
            this._offstage = offstage;
        }

        public bool offstage {
            get => this._offstage;

            set {
                if (value == this._offstage) {
                    return;
                }

                this._offstage = value;
                this.markNeedsLayoutForSizedByParentChange();
            }
        }
        bool _offstage;
        
        protected override double computeMinIntrinsicWidth(double height) {
            if (this.offstage)
                return 0.0;
            return base.computeMinIntrinsicWidth(height);
        }
        
        
        protected override double computeMaxIntrinsicWidth(double height) {
            if (this.offstage)
                return 0.0;
            return base.computeMaxIntrinsicWidth(height);
        }

        protected override double computeMinIntrinsicHeight(double width) {
            if (this.offstage)
                return 0.0;
            return base.computeMinIntrinsicHeight(width);
        }

        protected override double computeMaxIntrinsicHeight(double width) {
            if (this.offstage)
                return 0.0;
            return base.computeMaxIntrinsicHeight(width);
        }

        protected override double? computeDistanceToActualBaseline(TextBaseline baseline) {
            if (this.offstage)
                return null;
            return base.computeDistanceToActualBaseline(baseline);
        }

        protected override bool sizedByParent => this.offstage;

        protected override void performResize() {
            D.assert(this.offstage);
            this.size = this.constraints.smallest;
        }

        protected override void performLayout() {
            if (this.offstage) {
                this.child?.layout(this.constraints);
            } else {
                base.performLayout();
            }
        }

        public override bool hitTest(HitTestResult result, Offset position) {
            return !this.offstage && base.hitTest(result, position: position);
        }
    
        public override void paint(PaintingContext context, Offset offset) {
            if (this.offstage)
                return;
            base.paint(context, offset);
        }
    
    
        public override void debugFillProperties(DiagnosticPropertiesBuilder properties) {
            base.debugFillProperties(properties);
            properties.add(new DiagnosticsProperty<bool>("offstage", this.offstage));
        }
    
    
        public override List<DiagnosticsNode> debugDescribeChildren() {
            if (this.child == null)
                return new List<DiagnosticsNode>();
            return new List<DiagnosticsNode> {
                this.child.toDiagnosticsNode(
                    name: "child",
                    style: this.offstage ? DiagnosticsTreeStyle.offstage : DiagnosticsTreeStyle.sparse
                ),
            };
        }
    }
    
    public class RenderAbsorbPointer : RenderProxyBox {
          
        public RenderAbsorbPointer(
            RenderBox child = null,
            bool absorbing = true
          ) : base(child)
          {
              this._absorbing = absorbing;
            }


        public bool absorbing
        {
            get { return this._absorbing; }
            set
            {
                this._absorbing = value;
            }
        }
        bool _absorbing;
     
        public override bool hitTest(HitTestResult result, Offset position) {
            return this.absorbing
                ? this.size.contains(position)
                : base.hitTest(result, position: position);
        }
    
     
      public override void debugFillProperties(DiagnosticPropertiesBuilder properties) {
        base.debugFillProperties(properties);
        properties.add(new DiagnosticsProperty<bool>("absorbing", this.absorbing));
      }
    }
    
    public class RenderMetaData : RenderProxyBoxWithHitTestBehavior {
        public RenderMetaData(
            object metaData,
            HitTestBehavior behavior = HitTestBehavior.deferToChild,
            RenderBox child = null
        ) : base(behavior, child) {
            this.metaData = metaData;
        }
        
        public object metaData;

        public override void debugFillProperties(DiagnosticPropertiesBuilder properties) {
            base.debugFillProperties(properties);
            properties.add(new DiagnosticsProperty<object>("metaData", this.metaData));
        }
    }
}