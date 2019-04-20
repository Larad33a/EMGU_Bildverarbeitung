Public Class MyPoint
    Private _X As Int32
    Public Property X() As Int32
        Get
            Return _X
        End Get
        Set(ByVal value As Int32)
            _X = value
        End Set
    End Property
    Private _Y As Int32
    Public Property Y() As Int32
        Get
            Return _Y
        End Get
        Set(ByVal value As Int32)
            _Y = value
        End Set
    End Property
    Private _Z As Int32
    Public Property Z() As Int32
        Get
            Return _Z
        End Get
        Set(ByVal value As Int32)
            _Z = value
        End Set
    End Property

    Sub New()

    End Sub

    Sub New(x As Int32, y As Int32, Optional z As Int32 = 0)
        _X = x
        _Y = y
        _Z = z
    End Sub

    Public Overrides Function ToString() As String
        Return ($" {_X,4} | {_Y,4} | {_Z,4} ")
    End Function
End Class
