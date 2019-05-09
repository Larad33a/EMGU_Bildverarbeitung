Public Class IndexFläche
    Private _Index As Int32
    Public Property index() As Int32
        Get
            Return _Index
        End Get
        Set(ByVal value As Int32)
            _Index = value
        End Set
    End Property
    Private _Fläche As Double
    Public Property fläche() As Double
        'Test
        Get
            Return _Fläche
        End Get
        Set(ByVal value As Double)
            _Fläche = value
        End Set
    End Property

    Sub New(_ValIndex As Int32, _ValFläche As Double)
        _Index = _ValIndex
        _Fläche = _ValFläche
    End Sub
End Class
