//------------------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------



namespace Microsoft.Samples.Kinect.SpeechBasics
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.IO;
    using System.Text;
    using System.Windows;
    using System.Windows.Documents;
    using System.Windows.Media;
    using Microsoft.Kinect;
    using Microsoft.Speech.AudioFormat;
    using Microsoft.Speech.Recognition;
    using System.Windows.Media.Imaging;

    using System.Linq;
    using System.Net;
    using System.Net.Sockets;

    

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable",
        Justification = "In a full-fledged application, the SpeechRecognitionEngine object should be properly disposed. For the sake of simplicity, we're omitting that code in this sample.")]
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Resource key for medium-gray-colored brush.
        /// </summary>
        private const string MediumGreyBrushKey = "MediumGreyBrush";

        /// <summary>
        /// Map between each direction and the direction immediately to its right.
        /// </summary>
        /*private static readonly Dictionary<Direction, Direction> TurnRight = new Dictionary<Direction, Direction>
            {
                { Direction.Up, Direction.Right },
                { Direction.Right, Direction.Down },
                { Direction.Down, Direction.Left },
                { Direction.Left, Direction.Up }
            };*/

        /// <summary>
        /// Map between each direction and the direction immediately to its left.
        /// </summary>
        private static readonly Dictionary<Direction, Direction> TurnLeft = new Dictionary<Direction, Direction>
            {
                { Direction.Up, Direction.Left },
                { Direction.Right, Direction.Up },
                { Direction.Down, Direction.Right },
                { Direction.Left, Direction.Down }
            };

        /// <summary>
        /// Map between each direction and the displacement unit it represents.
        /// </summary>
        private static readonly Dictionary<Direction, Point> Displacements = new Dictionary<Direction, Point>
            {
                { Direction.Up, new Point { X = 0, Y = -1 } },
                { Direction.Right, new Point { X = 1, Y = 0 } },
                { Direction.Down, new Point { X = 0, Y = 1 } },
                { Direction.Left, new Point { X = -1, Y = 0 } }
            };
        /// <summary>
        /// Width of output drawing
        /// </summary>
        private const float RenderWidth = 640.0f;

        /// <summary>
        /// Height of our output drawing
        /// </summary>
        private const float RenderHeight = 480.0f;

        /// <summary>
        /// Thickness of drawn joint lines
        /// </summary>
        private const double JointThickness = 3;

        /// <summary>
        /// Thickness of body center ellipse
        /// </summary>
        private const double BodyCenterThickness = 10;

        /// <summary>
        /// Thickness of clip edge rectangles
        /// </summary>
        private const double ClipBoundsThickness = 10;

        /// <summary>
        /// Brush used to draw skeleton center point
        /// </summary>
        private readonly Brush centerPointBrush = Brushes.Blue;

        /// <summary>
        /// Brush used for drawing joints that are currently tracked
        /// </summary>
        private readonly Brush trackedJointBrush = new SolidColorBrush(Color.FromArgb(255, 68, 192, 68));

        //Piceles colores mano.
        private Brush brushManoIzq = Brushes.Aquamarine;
        private Brush brushManoDer = Brushes.Yellow;
        private const double JointThicknessAzul = 20;
        private const double JointThicknessOro = 20;

        private Brush brushRojo = Brushes.Red;
        private Brush brushAmarillo = Brushes.Yellow;
        private Brush brushVerde = Brushes.Green;
        private Brush brushAguamarina = Brushes.Aquamarine;
        private Brush brushAzul = Brushes.Blue;
        private Brush brushRosa  = Brushes.Pink;
        private Brush brushBlanco = Brushes.White;
        public TcpClient tcpclnt;
        byte rActual = 0;
        byte gActual = 0;
        byte bActual = 0;
        public bool estadoBombilla = false;
        /// <summary>
        /// Brush used for drawing joints that are currently inferred
        /// </summary>        
        private readonly Brush inferredJointBrush = Brushes.Yellow;

        /// <summary>
        /// Pen used for drawing bones that are currently tracked
        /// </summary>
        private readonly Pen trackedBonePen = new Pen(Brushes.Green, 6);
        private readonly Pen trackedBonePen_b = new Pen(Brushes.Red, 6);
       
       
        private readonly Brush brushPechoIzqda = Brushes.Gold;
        private readonly Brush brushPechoDerecha = Brushes.Pink;
        private float miAlturaIzqda = 0;
        private float miAlturaDer = 0;
        private float miAlturaCabeza = 0;
        public float distanciaDer = 0;
        public float distanciaIzq = 0;
        public float xPecho = 0;
        public float yPecho = 0;
        public float distanciaPechoCabeza = 0;
        public float xCabeza = 0;
        public float yCabeza = 0;
        public bool conectadoTCP=false;
        // Color colorIzq = new Color();

        /// <summary>
        /// Pen used for drawing bones that are currently inferred
        /// </summary>        
        private readonly Pen inferredBonePen = new Pen(Brushes.Gray, 1);
        /// <summary>
        /// Active Kinect sensor.
        /// </summary>
        private KinectSensor sensor;
        private DrawingGroup drawingGroup;
        private DrawingImage imageSource;
        //private TextBlock textValues = new TextBlock();

        /// <summary>
        /// Speech recognition engine using audio data from Kinect.
        /// </summary>
        private SpeechRecognitionEngine speechEngine;

        /// <summary>
        /// Current direction where turtle is facing.
        /// </summary>
        //private Direction curDirection = Direction.Up;

        /// <summary>
        /// List of all UI span elements used to select recognized text.
        /// </summary>
        private List<Span> recognitionSpans;
        //private List<Span> valoresSpans;
        //private List<Span> controlSpans;
        public bool depurando = false;
        /// <summary>
        /// Initializes a new instance of the MainWindow class.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Enumeration of directions in which turtle may be facing.
        /// </summary>
        private enum Direction
        {
            Up,
            Down,
            Left,
            Right
        }
        
        /// <summary>
        /// Gets the metadata for the speech recognizer (acoustic model) most suitable to
        /// process audio from Kinect device.
        /// </summary>
        /// <returns>
        /// RecognizerInfo if found, <code>null</code> otherwise.
        /// </returns>
        private static RecognizerInfo GetKinectRecognizer()
        {
            foreach (RecognizerInfo recognizer in SpeechRecognitionEngine.InstalledRecognizers())
            {
                string value;
                recognizer.AdditionalInfo.TryGetValue("Kinect", out value);
                if ("True".Equals(value, StringComparison.OrdinalIgnoreCase) && "es-ES".Equals(recognizer.Culture.Name, StringComparison.OrdinalIgnoreCase))
                {
                    return recognizer;
                }
            }
            
            return null;
        }

        private static void RenderClippedEdges(Skeleton skeleton, DrawingContext drawingContext)
        {
            if (skeleton.ClippedEdges.HasFlag(FrameEdges.Bottom))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, RenderHeight - ClipBoundsThickness, RenderWidth, ClipBoundsThickness));
            }

            if (skeleton.ClippedEdges.HasFlag(FrameEdges.Top))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, 0, RenderWidth, ClipBoundsThickness));
            }

            if (skeleton.ClippedEdges.HasFlag(FrameEdges.Left))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, 0, ClipBoundsThickness, RenderHeight));
            }

            if (skeleton.ClippedEdges.HasFlag(FrameEdges.Right))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(RenderWidth - ClipBoundsThickness, 0, ClipBoundsThickness, RenderHeight));
            }
        }

        /// <summary>
        /// Execute initialization tasks.
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            this.drawingGroup = new DrawingGroup();
            this.Esqueleto.Visibility = Visibility.Visible;
            this.Imagen.Visibility = Visibility.Visible;
            // Create an image source that we can use in our image control
            this.imageSource = new DrawingImage(this.drawingGroup);
            //valoresSpans = new List<Span> { RightValue, LeftValue };
            //controlSpans = new List<Span> { RightSpan, LeftSpan };
            // Display the drawing using our image control
            Esqueleto.Source = this.imageSource;
            //Imagen.Source = this.imageSource;
            //colorIzq = Color.FromRgb(255, 255, 255);
            //colorActual.r = 0;
            

            // Look through all sensors and start the first connected one.
            // This requires that a Kinect is connected at the time of app startup.
            // To make your app robust against plug/unplug, 
            // it is recommended to use KinectSensorChooser provided in Microsoft.Kinect.Toolkit (See components in Toolkit Browser).
            foreach (var potentialSensor in KinectSensor.KinectSensors)
            {
                if (potentialSensor.Status == KinectStatus.Connected)
                {
                    this.sensor = potentialSensor;
                    break;
                }
            }

            if (null != this.sensor)
            {
                sensor.ColorStream.Enable();
                sensor.ColorFrameReady += myKinect_ColorFrameReady;
                // Start the sensor!
                // Turn on the skeleton stream to receive skeleton frames
                this.sensor.SkeletonStream.Enable();

                // Add an event handler to be called whenever there is new color frame data
                this.sensor.SkeletonFrameReady += this.SensorSkeletonFrameReady;
                sensor = KinectSensor.KinectSensors[0];

                try
                {
                    // Start the sensor!
                    this.sensor.Start();
                }
                catch (IOException)
                {
                    // Some other application is streaming from the same Kinect sensor
                    this.sensor = null;
                }
            }

            if (null == this.sensor)
            {
                this.statusBarText.Text = Properties.Resources.NoKinectReady;
                return;
            }

            RecognizerInfo ri = GetKinectRecognizer();

            if (null != ri)
            {
                recognitionSpans = new List<Span> { forwardSpan, backSpan };

                this.speechEngine = new SpeechRecognitionEngine(ri.Id);

                /****************************************************************
                * 
                * Use this code to create grammar programmatically rather than from
                * a grammar file.
                */ 
                 var directions = new Choices();
                 directions.Add(new SemanticResultValue("luz", "FORWARD"));
                 directions.Add(new SemanticResultValue("lus", "FORWARD"));
                 directions.Add(new SemanticResultValue("lumus", "FORWARD"));
                directions.Add(new SemanticResultValue("enciende", "FORWARD"));
                directions.Add(new SemanticResultValue("encender", "FORWARD"));
                directions.Add(new SemanticResultValue("encendido", "FORWARD"));
                directions.Add(new SemanticResultValue("encendio", "FORWARD"));

               // directions.Add(new SemanticResultValue("apagar", "BACKWARD"));
                 directions.Add(new SemanticResultValue("apagao", "BACKWARD"));
                directions.Add(new SemanticResultValue("apagado", "BACKWARD"));
               // directions.Add(new SemanticResultValue("apaga", "BACKWARD"));
               // directions.Add(new SemanticResultValue("abajo", "BACKWARD"));


                directions.Add(new SemanticResultValue("turn left", "LEFT"));
                directions.Add(new SemanticResultValue("izquierda", "LEFT"));

                directions.Add(new SemanticResultValue("derecha", "RIGHT"));
                
                 var gb = new GrammarBuilder { Culture = ri.Culture };
                 gb.Append(directions);
                
                 var g = new Grammar(gb);
                /* 
                ****************************************************************/

                // Create a grammar from grammar definition XML file.
                using (var memoryStream = new MemoryStream(Encoding.ASCII.GetBytes(Properties.Resources.SpeechGrammar)))
                {
                   // var g = new Grammar(memoryStream);
                    speechEngine.LoadGrammar(g);
                }

                speechEngine.SpeechRecognized += SpeechRecognized;
                speechEngine.SpeechRecognitionRejected += SpeechRejected;

                // For long recognition sessions (a few hours or more), it may be beneficial to turn off adaptation of the acoustic model. 
                // This will prevent recognition accuracy from degrading over time.
                ////speechEngine.UpdateRecognizerSetting("AdaptationOn", 0);

                speechEngine.SetInputToAudioStream(
                    sensor.AudioSource.Start(), new SpeechAudioFormatInfo(EncodingFormat.Pcm, 16000, 16, 1, 32000, 2, null));
                speechEngine.RecognizeAsync(RecognizeMode.Multiple);
            }
            else
            {
                this.statusBarText.Text = Properties.Resources.NoSpeechRecognizer;
            }
        }

        /// <summary>
        /// Execute uninitialization tasks.
        /// </summary>
        /// <param name="sender">object sending the event.</param>
        /// <param name="e">event arguments.</param>
        private void WindowClosing(object sender, CancelEventArgs e)
        {
            if (null != this.sensor)
            {
                this.sensor.AudioSource.Stop();

                this.sensor.Stop();
                this.sensor = null;
            }

            if (null != this.speechEngine)
            {
                this.speechEngine.SpeechRecognized -= SpeechRecognized;
                this.speechEngine.SpeechRecognitionRejected -= SpeechRejected;
                this.speechEngine.RecognizeAsyncStop();
            }
            try
            {
                if (tcpclnt.Connected) tcpclnt.Close();

            }
            catch (Exception exceptio) {

            }
        }

        /// <summary>
        /// Remove any highlighting from recognition instructions.
        /// </summary>
        private void ClearRecognitionHighlights()
        {
            foreach (Span span in recognitionSpans)
            {
                span.Foreground = (Brush)this.Resources[MediumGreyBrushKey];
                span.FontWeight = FontWeights.Normal;
            }
            /*foreach (Span span in valoresSpans)
            {
                span.Foreground = (Brush)this.Resources[MediumGreyBrushKey];
                span.FontWeight = FontWeights.Normal;
            }
            foreach (Span span in controlSpans)
            {
                span.Foreground = (Brush)this.Resources[MediumGreyBrushKey];
                span.FontWeight = FontWeights.Normal;
            }*/
        }

        /// <summary>
        /// Handler for recognized speech events.
        /// </summary>
        /// <param name="sender">object sending the event.</param>
        /// <param name="e">event arguments.</param>
        /// 
        void myKinect_ColorFrameReady(object sender, ColorImageFrameReadyEventArgs e)
        {
            using (ColorImageFrame ColorFrame = e.OpenColorImageFrame())
            {
                if (ColorFrame == null) return;
                byte[] ColorData = new byte[ColorFrame.PixelDataLength];
                ColorFrame.CopyPixelDataTo(ColorData);
                Imagen.Source = BitmapSource.Create(
                    ColorFrame.Width, ColorFrame.Height,
                    500, 500,
                    PixelFormats.Bgr32,
                    null,
                    ColorData,
                    ColorFrame.Width * ColorFrame.BytesPerPixel
                    );
            }
        }
        private void SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            // Speech utterance confidence below which we treat speech as if it hadn't been heard
            const double ConfidenceThreshold = 0.3;

            // Number of degrees in a right angle.
            //const int DegreesInRightAngle = 90;

            // Number of pixels turtle should move forwards or backwards each time.
            //const int DisplacementAmount = 60;

            ClearRecognitionHighlights();

            if (e.Result.Confidence >= ConfidenceThreshold)
            {
                switch (e.Result.Semantics.Value.ToString())
                {
                    case "FORWARD":
                        forwardSpan.Foreground = Brushes.DeepSkyBlue;
                        forwardSpan.FontWeight = FontWeights.Bold;
                        string temporal = "Derecha :\t" + distanciaDer.ToString() + "\n" + "Izquierda :\t" + distanciaIzq.ToString();
                        if (depurando)  MessageBox.Show(temporal);

                        if (conectadoTCP)
                       {
                           //"Enter the string to be transmitted : ");
                           int longitud = 0;
                           String str = "E" + "XXXXXXXXXXXX"; //Console.ReadLine();
                           str += '\n';
                           Stream stm = tcpclnt.GetStream();

                           estadoBombilla = true;
                           ASCIIEncoding asen = new ASCIIEncoding();
                           byte[] ba = asen.GetBytes(str);
                           //Console.WriteLine("Transmitting...");

                           stm.Write(ba, 0, ba.Length);

                       }
                        break;
                    case "BACKWARD":
                        backSpan.Foreground = Brushes.DeepSkyBlue;
                        backSpan.FontWeight = FontWeights.Bold;
                        if (depurando) MessageBox.Show("Apagado");
                        if (conectadoTCP)
                        {
                            //"Enter the string to be transmitted : ");
                            int longitud = 0;
                            String str = "A" + "XXXXXXXXXXXX"; //Console.ReadLine();
                            str += '\n';
                            Stream stm = tcpclnt.GetStream();
                            estadoBombilla = false;
                            ASCIIEncoding asen = new ASCIIEncoding();
                            byte[] ba = asen.GetBytes(str);
                            //Console.WriteLine("Transmitting...");

                            stm.Write(ba, 0, ba.Length);


                        }
                        break;
                    case "LEFT":
                        if (depurando) MessageBox.Show(distanciaIzq.ToString());
                        /*RightSpan.Foreground = Brushes.DeepSkyBlue;
                        RightSpan.FontWeight = FontWeights.Bold;*/
                        //RightValue.SetValue(distanciaDer.ToString());

                        break;
                    case "RIGHT":
                        /*LeftSpan.Foreground = Brushes.DeepSkyBlue;
                        LeftSpan.FontWeight = FontWeights.Bold;*/
                        //textValues.Text.ToString;
                        
                        if (depurando) MessageBox.Show(distanciaDer.ToString());

                        break;
                    default:
                        if (depurando) MessageBox.Show("DEFAULT");
                        break;
                }
            }
        }

        /// <summary>
        /// Handler for rejected speech events.
        /// </summary>
        /// <param name="sender">object sending the event.</param>
        /// <param name="e">event arguments.</param>
        private void SpeechRejected(object sender, SpeechRecognitionRejectedEventArgs e)
        {
            ClearRecognitionHighlights();
        }
        /// <summary>
        /// Draws a skeleton's bones and joints
        /// </summary>
        /// <param name="skeleton">skeleton to draw</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        private void DrawBonesAndJoints(Skeleton skeleton, DrawingContext drawingContext)
        {
            // Render Torso
            this.DrawBone(skeleton, drawingContext, JointType.Head, JointType.ShoulderCenter);
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderCenter, JointType.ShoulderLeft);
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderCenter, JointType.ShoulderRight);
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderCenter, JointType.Spine);
            this.DrawBone(skeleton, drawingContext, JointType.Spine, JointType.HipCenter);
            this.DrawBone(skeleton, drawingContext, JointType.HipCenter, JointType.HipLeft);
            this.DrawBone(skeleton, drawingContext, JointType.HipCenter, JointType.HipRight);

            // Left Arm
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderLeft, JointType.ElbowLeft);
            this.DrawBone(skeleton, drawingContext, JointType.ElbowLeft, JointType.WristLeft);
            this.DrawBone(skeleton, drawingContext, JointType.WristLeft, JointType.HandLeft);

            // Right Arm
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderRight, JointType.ElbowRight);
            this.DrawBone(skeleton, drawingContext, JointType.ElbowRight, JointType.WristRight);
            this.DrawBone(skeleton, drawingContext, JointType.WristRight, JointType.HandRight);

            // Left Leg
            this.DrawBone(skeleton, drawingContext, JointType.HipLeft, JointType.KneeLeft);
            this.DrawBone(skeleton, drawingContext, JointType.KneeLeft, JointType.AnkleLeft);
            this.DrawBone(skeleton, drawingContext, JointType.AnkleLeft, JointType.FootLeft);

            // Right Leg
            this.DrawBone(skeleton, drawingContext, JointType.HipRight, JointType.KneeRight);
            this.DrawBone(skeleton, drawingContext, JointType.KneeRight, JointType.AnkleRight);
            this.DrawBone(skeleton, drawingContext, JointType.AnkleRight, JointType.FootRight);

            // Render Joints
            foreach (Joint joint in skeleton.Joints)
            {
                Brush drawBrush = null;

                if (joint.TrackingState == JointTrackingState.Tracked)
                {
                    drawBrush = this.trackedJointBrush;
                }
                else if (joint.TrackingState == JointTrackingState.Inferred)
                {
                    drawBrush = this.inferredJointBrush;
                }
                double miJointThickness = JointThickness;
                if (joint.JointType == JointType.HandLeft)
                {
                    miAlturaIzqda = joint.Position.Y;
                    if (miAlturaIzqda > miAlturaCabeza)
                    {
                        //drawBrush = this.brushManoIzq;
                        miJointThickness = JointThicknessAzul;
                        // this.Imagen.Visibility = Visibility.Visible;
                        float temporalIzq = (xPecho - joint.Position.X)/(distanciaPechoCabeza * (float)2);
                        if (temporalIzq < 0.3)
                        {
                            distanciaIzq = 0;
                        }
                        else if (temporalIzq > 1.3) {
                            distanciaIzq = 1;
                        }
                        else
                        {
                            distanciaIzq = temporalIzq - (float)0.3;
                        }

                        //Condiciones brushese brochas
                        if (0 >= distanciaIzq && distanciaIzq > 0.25)
                        {
                           // drawBrush = this.brushBlanco;
                            rActual =255;
                            gActual =255;
                            bActual =255;
                        }
                        else if (0.25 <= distanciaIzq && distanciaIzq < 0.375) {
                            //drawBrush = this.brushRojo;
                            rActual = 255;
                            gActual = 0;
                            bActual = (byte)(255 - (((distanciaIzq - 0.25) * 255) / 0.125));
                        }
                        else if (0.375 <= distanciaIzq && distanciaIzq < 0.5) {
                            //drawBrush = this.brushAmarillo;
                            rActual = 255;
                            gActual = (byte)(((distanciaIzq - 0.375) * 255) / 0.125); 
                            bActual = 0;
                        }
                        else if (0.5 <= distanciaIzq && distanciaIzq < 0.625){
                            //drawBrush = this.brushVerde;
                            rActual = (byte) (255 -(((distanciaIzq - 0.5)*255)/ 0.125));
                            gActual = 255;
                            bActual = 0;
                        }
                        else if (0.625 <= distanciaIzq && distanciaIzq < 0.75){
                            //drawBrush = this.brushAguamarina;
                            rActual = 0;
                            gActual = 255;
                            bActual = (byte)(((distanciaIzq - 0.625) * 255) / 0.125);
                        }
                        else if (0.75 <= distanciaIzq && distanciaIzq < 0.875){
                            //drawBrush = this.brushAzul;
                            rActual = 0;
                            gActual = (byte)(255 - (((distanciaIzq - 0.75) * 255) / 0.125));
                            bActual = 255;
                        }
                        else if (0.875 <= distanciaIzq && distanciaIzq < 1){
                            //drawBrush = this.brushRosa;
                            rActual = (byte)(((distanciaIzq - 0.875) * 255) / 0.125);
                            gActual = 0;
                            bActual = 255;
                        }
                        else{
                           // drawBrush = this.brushBlanco;
                            rActual = 255;
                            gActual = 255;
                            bActual = 255;
                        }
                        //LeftValue.SetCurrentValue(MainWindow,distanciaIzq.ToString());
                        colorValueButton.Background = new SolidColorBrush(Color.FromRgb(rActual,gActual,bActual));
                        drawBrush= new SolidColorBrush(Color.FromRgb(rActual, gActual, bActual));
                        ColorValueR.Text = rActual.ToString();
                        ColorValueG.Text = gActual.ToString();
                        ColorValueB.Text = bActual.ToString();
                        //Aquí los datos de color
                        
                        if (conectadoTCP && estadoBombilla){
                            //"Enter the string to be transmitted : ");
                            int longitudm = 0;
                                String str = "C" + "R" ; //Console.ReadLine();
                            if (rActual < 10){
                                str += "00" + rActual.ToString();
                            }
                            else if (rActual < 100){
                                str += "0" + rActual.ToString();
                            }
                            else{
                                str += rActual.ToString();
                            }

                            str += "G";
                            if (gActual < 10){
                                str += "00" + gActual.ToString();
                            }
                            else if (gActual < 100){
                                str += "0" + gActual.ToString();
                            }
                            else{
                                str += gActual.ToString();
                            }
                            str += "B";
                            if (bActual < 10){
                                str += "00" + bActual.ToString();
                            }
                            else if (bActual < 100){
                                str += "0" + bActual.ToString();
                            }
                            else{
                                str += bActual.ToString();
                            }
                            Stream stm = tcpclnt.GetStream();
                           
                            str += '\n';
                            ASCIIEncoding asen = new ASCIIEncoding();
                            byte[] ba = asen.GetBytes(str);
                            //Console.WriteLine("Transmitting...");

                            stm.Write(ba, 0, ba.Length);

                            /* byte[] bb = new byte[100];
                            int k = stm.Read(bb, 0, 100);*/

                        }
                        

                    }
                    else{
                        //this.Imagen.Visibility = Visibility.Hidden;

                    }
                }
                if (joint.JointType == JointType.HandRight){
                    miAlturaDer = joint.Position.Y;
                    if (miAlturaDer > miAlturaCabeza){
                        //drawBrush = this.brushManoDer;
                        miJointThickness = JointThicknessOro;
                        float temporalDer = (joint.Position.X - xPecho)/(distanciaPechoCabeza * (float)2);
                        if (temporalDer < 0.3){
                            distanciaDer = 0;
                        }
                        else if (temporalDer > 1.3){
                            distanciaDer = 1;
                        }
                        else{
                            distanciaDer = temporalDer - (float)0.3;
                        }
                        BrilloValue.Text = ((int)(distanciaDer * 255)).ToString();
                        drawBrush = new SolidColorBrush(Color.FromRgb((byte)(distanciaDer * 255), (byte)(distanciaDer * 255),
                            (byte)(distanciaDer * 255)));
                        
                            if (conectadoTCP && estadoBombilla){
                            //"Enter the string to be transmitted : ");
                                int longitud = 0;
                                String str = "L" ; //Console.ReadLine();
                            if (distanciaDer*255 < 10){
                                str += "00" + ((byte)(distanciaDer*255)).ToString();
                            }
                            else if (distanciaDer *255 < 100){
                                str += "0" + ((byte)(distanciaDer * 255)).ToString();
                            }
                            else{
                                str += ((byte)(distanciaDer * 255)).ToString();
                            }
                            longitud = 14 - str.Length;
                            while (longitud > 1){
                                longitud--;
                                str += "X";
                            }
                            str += '\n';
                                Stream stm = tcpclnt.GetStream();

                                ASCIIEncoding asen = new ASCIIEncoding();
                                byte[] ba = asen.GetBytes(str);
                                //Console.WriteLine("Transmitting...");

                                stm.Write(ba, 0, ba.Length);

                               /* *byte[] bb = new byte[100];
                                int k = stm.Read(bb, 0, 100);*/
                            }
                        
                    // aquí el dato de Brillo (byte)(distanciaDer * 255)
                }
                    else{
                        //this.Esqueleto.Visibility = Visibility.Hidden;
                    }
                }
                else if (joint.JointType == JointType.Head){
                    yCabeza=miAlturaCabeza = joint.Position.Y;
                    xCabeza = joint.Position.X;
                }
                if (joint.JointType == JointType.ShoulderCenter){
                    miJointThickness = JointThicknessAzul;
                    xPecho = joint.Position.X;
                    yPecho = joint.Position.Y;
                    distanciaPechoCabeza =(float) Math.Sqrt(Math.Pow((yPecho-yCabeza),2) + Math.Pow(xPecho-xCabeza, 2));
                    pechoValue.Text = distanciaPechoCabeza.ToString();

                    Point miPuntoPecho = this.SkeletonPointToScreen(joint.Position);
                    if (miPuntoPecho.X > RenderWidth / 2) // derecha
                    {
                        drawBrush = brushPechoDerecha;
                    }
                    else // Izqda
                    {
                        drawBrush = brushPechoIzqda;
                    }
                }

                if (drawBrush != null)
                {
                    drawingContext.DrawEllipse(drawBrush, null, this.SkeletonPointToScreen(joint.Position), miJointThickness, miJointThickness);
                }
            }
        }
        private Point SkeletonPointToScreen(SkeletonPoint skelpoint)
        {
            // Convert point to depth space.  
            // We are not using depth directly, but we do want the points in our 640x480 output resolution.
            
                DepthImagePoint depthPoint = this.sensor.CoordinateMapper.MapSkeletonPointToDepthPoint(skelpoint, DepthImageFormat.Resolution640x480Fps30);
                return new Point(depthPoint.X, depthPoint.Y);

        }

        /// <summary>
        ///// Draws a bone line between two joints
        /// </summary>
        /// <param name="skeleton">skeleton to draw bones from</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        /// <param name="jointType0">joint to start drawing from</param>
        /// <param name="jointType1">joint to end drawing at</param>
        private void DrawBone(Skeleton skeleton, DrawingContext drawingContext, JointType jointType0, JointType jointType1)
        {
            Joint joint0 = skeleton.Joints[jointType0];
            Joint joint1 = skeleton.Joints[jointType1];

            // If we can't find either of these joints, exit
            if (joint0.TrackingState == JointTrackingState.NotTracked ||
                joint1.TrackingState == JointTrackingState.NotTracked)
            {
                return;
            }

            // Don't draw if both points are inferred
            if (joint0.TrackingState == JointTrackingState.Inferred &&
                joint1.TrackingState == JointTrackingState.Inferred)
            {
                return;
            }

            // We assume all drawn bones are inferred unless BOTH joints are tracked
            Pen drawPen = this.inferredBonePen;
            if (joint0.TrackingState == JointTrackingState.Tracked && joint1.TrackingState == JointTrackingState.Tracked)
            {
                drawPen = this.trackedBonePen;
            }
            if (joint1.JointType == JointType.Head || joint0.JointType == JointType.Head)
            {
                drawPen = this.trackedBonePen_b;
            }

            drawingContext.DrawLine(drawPen, this.SkeletonPointToScreen(joint0.Position), this.SkeletonPointToScreen(joint1.Position));
        }

        /// <summary>
        /// Handles the checking or unchecking of the seated mode combo box
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void CheckBoxSeatedModeChanged(object sender, RoutedEventArgs e)
        {
            if (null != this.sensor)
            {
                if (this.checkBoxSeatedMode.IsChecked.GetValueOrDefault()){
                    this.sensor.SkeletonStream.TrackingMode = SkeletonTrackingMode.Seated;
                }else{
                    this.sensor.SkeletonStream.TrackingMode = SkeletonTrackingMode.Default;
                }
            }
        }
        private void SensorSkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            Skeleton[] skeletons = new Skeleton[0];

            using (SkeletonFrame skeletonFrame = e.OpenSkeletonFrame())
            {
                if (skeletonFrame != null)
                {
                    skeletons = new Skeleton[skeletonFrame.SkeletonArrayLength];
                    skeletonFrame.CopySkeletonDataTo(skeletons);
                }
            }

            using (DrawingContext dc = this.drawingGroup.Open())
            {
                // Draw a transparent background to set the render size
                dc.DrawRectangle(Brushes.Black, null, new Rect(0.0, 0.0, RenderWidth, RenderHeight));

                if (skeletons.Length != 0)
                {
                    foreach (Skeleton skel in skeletons)
                    {
                        RenderClippedEdges(skel, dc);

                        if (skel.TrackingState == SkeletonTrackingState.Tracked)
                        {
                            this.DrawBonesAndJoints(skel, dc);
                        }
                        else if (skel.TrackingState == SkeletonTrackingState.PositionOnly)
                        {
                            dc.DrawEllipse(
                            this.centerPointBrush,
                            null,
                            this.SkeletonPointToScreen(skel.Position),
                            BodyCenterThickness,
                            BodyCenterThickness);
                        }
                    }
                }

                // prevent drawing outside of our render area
                this.drawingGroup.ClipGeometry = new RectangleGeometry(new Rect(0.0, 0.0, RenderWidth, RenderHeight));
            }
        }

    
        private void TextInput_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
                try
                {
                    string ip;
                    /*using (System.IO.StreamReader file = new System.IO.StreamReader("conf.ini"))
                    {
                        ip = file.ReadLine();
                    }*/
                    ip = textInput.Text;
                    if (ip.Length < 16 && ip.Length > 10)
                    {
                        IPAddress ipAd = IPAddress.Parse(ip);
                        //this.estadoTCP.Text = "Conectando";
                        estadoTCP.Text = "Conectando";
                        tcpclnt = new TcpClient();
                        estadoTCP.Text = "Conectando con " + ip + " por el puerto 8010";
                        tcpclnt.Connect(ip, 8010);
                        //texto1.Text = "oatata";
                        if (tcpclnt.Connected)
                        {
                            estadoTCP.Text = "Conectado";
                            conectadoTCP = true;
                        }
                        else
                        {
                            estadoTCP.Text = "No conectado";
                            conectadoTCP = false;
                        }
                    }
                    else
                    {
                        //Caracteres insuficientes
                        estadoTCP.Text = "Error de formato";
                    }
                }
                catch (SocketException err)
                {
                    estadoTCP.Text = "Error: " + err.StackTrace;
                }
                catch (Exception err)
                {
                    estadoTCP.Text = "Error: " + err.StackTrace;
                }
                //TCP
        }

        private void BotonVerde_Click(object sender, RoutedEventArgs e)
        {
            if (conectadoTCP)
            {
                //"Enter the string to be transmitted : ");
                estadoBombilla = true;
                String str; //Console.ReadLine();
                str = "E" + "XXXXXXXXXXXX";
                str += '\n';
                Stream stm = tcpclnt.GetStream();
                ASCIIEncoding asen = new ASCIIEncoding();
                byte[] ba = asen.GetBytes(str);
                //Console.WriteLine("Transmitting...");
                stm.Write(ba, 0, ba.Length);


                str = "L255XXXXXXXXX";
                str += '\n';
                // asen = new ASCIIEncoding();
                ba = asen.GetBytes(str);
                stm.Write(ba, 0, ba.Length);

                str = "CR000G255B000";
                str += '\n';
                ba = asen.GetBytes(str);
                stm.Write(ba, 0, ba.Length);
            }
        }

        private void BotonAzul_Click(object sender, RoutedEventArgs e)
        {
            if (conectadoTCP)
            {
                //"Enter the string to be transmitted : ");
                estadoBombilla = true;
                String str; //Console.ReadLine();
                str = "E" + "XXXXXXXXXXXX";
                str += '\n';
                Stream stm = tcpclnt.GetStream();
                ASCIIEncoding asen = new ASCIIEncoding();
                byte[] ba = asen.GetBytes(str);
                //Console.WriteLine("Transmitting...");
                stm.Write(ba, 0, ba.Length);


                str = "L255XXXXXXXXX";
                str += '\n';
                // asen = new ASCIIEncoding();
                ba = asen.GetBytes(str);
                stm.Write(ba, 0, ba.Length);

                str = "CR000G000B255";
                str += '\n';
                ba = asen.GetBytes(str);
                stm.Write(ba, 0, ba.Length);
            }
        }

        private void BotonRojo_Click(object sender, RoutedEventArgs e)
        {
            if (conectadoTCP)
            {
                //"Enter the string to be transmitted : ");
                estadoBombilla = true;
                String str; //Console.ReadLine();
                str = "E" + "XXXXXXXXXXXX";
                str += '\n';
                Stream stm = tcpclnt.GetStream();
                ASCIIEncoding asen = new ASCIIEncoding();
                byte[] ba = asen.GetBytes(str);
                //Console.WriteLine("Transmitting...");
                stm.Write(ba, 0, ba.Length);


                str = "L255XXXXXXXXX";
                str += '\n';
               // asen = new ASCIIEncoding();
                ba = asen.GetBytes(str);
                stm.Write(ba, 0, ba.Length);

                str = "CR255G000B000";
                str += '\n';
                ba = asen.GetBytes(str);
                stm.Write(ba, 0, ba.Length);
            }
        }
    }
}