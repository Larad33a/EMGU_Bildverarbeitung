Public Class MyRefObjekt
    Private _PunktRobo As PointF
    Public Property PunktRobo() As PointF
        Get
            Return _PunktRobo
        End Get
        Set(ByVal PunktRobo As PointF)
            _PunktRobo = PunktRobo
        End Set
    End Property
    Private _PunktCam As PointF
    Public Property PunktCam() As PointF
        Get
            Return _PunktCam
        End Get
        Set(ByVal PunktCam As PointF)
            _PunktCam = PunktCam
        End Set
    End Property
    Private _ZinMM As Int32
    Public Property ZinMM() As Int32
        Get
            Return _ZinMM
        End Get
        Set(ByVal ZinMM As Int32)
            _ZinMM = ZinMM
        End Set
    End Property
    Private _ZinUnits As Int32
    Public Property ZinUnits() As Int32
        Get
            Return _ZinUnits
        End Get
        Set(ByVal ZinUnits As Int32)
            _ZinUnits = ZinUnits
        End Set
    End Property
    'XY
    Sub New(robo As PointF, kamera As PointF)
        _PunktRobo = robo
        _PunktCam = kamera
    End Sub
    Sub New(robox As Single, roboy As Single, kamerax As Single, kameray As Single)
        _PunktRobo = New PointF(robox, roboy)
        _PunktCam = New PointF(kamerax, kameray)
    End Sub
    'Z
    Sub New(zinmm As Int32, zinunits As Int32)
        _ZinMM = zinmm
        _ZinUnits = zinunits
    End Sub
    'Xr = Mx*Xc+Bx
    'Yr = My*Yc+By

    Public Overrides Function ToString() As String
        Return ($"R:({_PunktRobo.ToString(),5:n2}) | K:({_PunktCam.ToString(),5:n2})")
    End Function
End Class
