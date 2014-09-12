% %MATLAB_Webcam_Control

% Adapted C# Code
% ImageViewer viewer = new ImageViewer(); //create an image viewer
% Capture capture = new Capture(); //create a camera captue
% Application.Idle += new EventHandler(delegate(object sender, EventArgs e)
% {  //run this until application closed (close button click on image viewer)
%    viewer.Image = capture.QueryFrame(); //draw the image obtained from camera
% });
% viewer.ShowDialog(); //show the image viewer

% Load in the Namespaces
Namespace.EMGU_CV_UTIL = NET.addAssembly([pwd '\Emgu\Emgu.Util.dll']);
Namespace.EMGU_CV = NET.addAssembly([pwd '\Emgu\Emgu.CV.dll']);
Namespace.EMGU_CV_UI = NET.addAssembly([pwd '\Emgu\Emgu.CV.UI.dll']);
viewer = Emgu.CV.UI.ImageViewer; % Create Figure Window
viewer.Visible = true;
capture = Emgu.CV.Capture;

for iFrame = 1:100
    capture.Grab();
    Frame = capture.RetrieveBgrFrame();
    Frame = Frame.Clone(); % First plane = blue, second plane = green, third plane = red
    viewer.Image = Frame;
    Data = uint8(Frame.Data);
    pause(.01)
end