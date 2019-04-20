Option Strict On

Imports Intel.RealSense
Imports System.Runtime.InteropServices
Imports System.Threading 'Für Tasks zu beennden

Imports Emgu.CV
Imports Emgu.CV.CvEnum
Imports Emgu.CV.Structure
Imports Emgu.CV.UI
Imports Emgu.CV.Util

'Infos:
'Emgu.CV.CvInvoke. zugriff auf alle openCV befehle

Public Class Form_Main

    'Konstanten
    Const cfpsDepth = 6 'Möglich: 6,15,30,60
    Const cfpsColor = 6

    Const cWidht = 1280
    Const cHeight = 720

    Const cMaxTrys = 3
    Const cMaxRestart = 3

    Const cCamOffset = 70 '107

    Const Versatz_Bild_Tiefe_X = 100
    Const Versatz_Bild_Tiefe_Y = 100

    'Enums
    Public Enum Enum_Format
        color
        depth
    End Enum


    'Globale Variablen
    Private _MyPipeline As New Pipeline
    Private _MyCFG As New Config()
    Private _MyColorMap As New Colorizer()

    'TCPVariablen
    Private WithEvents _TcpVariablen As New TCPVariables


    'Konfigurations Variablen
    Private Konf_FPS_Color As Int32
    Private Konf_FPS_Depth As Int32

    Private Konf_Widht_Color As Int32
    Private Konf_Height_Color As Int32
    Private Konf_Widht_Depth As Int32
    Private Konf_Height_Depth As Int32

    Private Konf_Format_Color As Format
    Private Konf_Format_Depth As Format



    'Private _cts As New CancellationTokenSource '#### Für Tasks zu beennden
    Private _MyPipelineAktiv As Boolean
    Private _trys As Int32 = 0

    Private _DepthImgTaken As Boolean = False
    Private _DepthCImgTaken As Boolean = False
    Private _ColorImgTaken As Boolean = False
    Private _RefCImgTaken As Boolean = False
    Private _RefDImgTaken As Boolean = False

    Private _MyObjekte As New List(Of MyObekt)
    Private _MySearchObjekte As New List(Of SearchObj)


    'Mat
    Private _MatColor As Mat
    Private _MatDepth As Mat
    Private _MatDepthC As Mat
    Private _MatRefC As Mat
    Private _MatRefD As Mat
    Private _MatRefDc As Mat
    Private _DisColor As New Mat
    Private _DisDepth As New Mat
    Private _DisDepthC As New Mat
    Private _DisRefC As New Mat
    Private _DisRefD As New Mat
    Private _DisRes01 As New Mat
    Private _DisRes02 As New Mat

    Private _MatResult As New Mat



    '-----------------------------------------------------------------------------------------------------------------------
    'Constructor
    '-----------------------------------------------------------------------------------------------------------------------
    Sub New()

        ' Dieser Aufruf ist für den Designer erforderlich.
        InitializeComponent()

        ' Fügen Sie Initialisierungen nach dem InitializeComponent()-Aufruf hinzu.

    End Sub

    '-----------------------------------------------------------------------------------------------------------------------
    'Laden & schliesen der Form
    '-----------------------------------------------------------------------------------------------------------------------
    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        'Konfiguration vorbelegen
        'Farbe
        comb_Konf_Col_FPS.SelectedItem = 6
        comb_Konf_Col_Format.SelectedItem = Format.Bgr8
        comb_Konf_Col_Auflösung.SelectedItem = "640x420"

        'Tiefe
        comb_Konf_Col_FPS.SelectedItem = 6
        comb_Konf_Col_Format.SelectedItem = Format.Z16
        comb_Konf_Col_Auflösung.SelectedItem = "640x420"

        KonfigPipe()
        ImgStatus()
        Autoreferenz()
        'AddHandler btn_StartStop.Click, New EventHandler(AddressOf Me.ProcessFrameAndUpdateGUI)

    End Sub

    Private Sub Form1_FormClosed(sender As Object, e As FormClosedEventArgs) Handles MyBase.FormClosed

    End Sub

    '-----------------------------------------------------------------------------------------------------------------------
    'Button & Eingaben
    '-----------------------------------------------------------------------------------------------------------------------
    'Image
    Private Sub btn_NewImg_Click(sender As Object, e As EventArgs) Handles btn_NewImg.Click
        If TakePicture(_MatColor, _MatDepth, _MatDepthC) Then
            'Anzeigen
            CvInvoke.Resize(_MatColor, _DisColor, New Size(640, 480))
            ib_new_Color.Image = _DisColor.ToImage(Of Bgr, Byte)
            CvInvoke.Resize(_MatDepthC, _DisDepth, New Size(640, 480))
            ib_new_Depth.Image = _DisDepth.ToImage(Of Bgr, Byte)
            TC2_Bilder.SelectedTab = P1_NewImg

            TC2_Bilder.TabPages(3).Text = "Z16 Tiefenbild"
            ib_depth_01.Image = _MatDepth
        End If
    End Sub

    Private Sub btn_RefImg_Click(sender As Object, e As EventArgs) Handles btn_RefImg.Click
        If TakePicture(_MatRefC, _MatRefD, _MatRefDc) Then
            lb_Info.Items.Insert(0, "Referenzbilder Color erfolgreich")
            'Anzeigen
            CvInvoke.Resize(_MatRefC, _DisRefC, New Size(640, 480))
            ib_rev_Color.Image = _DisRefC.ToImage(Of Bgr, Byte)
            CvInvoke.Resize(_MatRefDc, _DisRefD, New Size(640, 480))
            ib_rev_Depth.Image = _DisRefD.ToImage(Of Bgr, Byte)
            TC2_Bilder.SelectedTab = P3_Rev

            TC2_Bilder.TabPages(3).Text = "Z16 Tiefenbild"
            ib_depth_01.Image = _MatDepth
        End If
    End Sub

    Private Sub btn_Reset_Click(sender As Object, e As EventArgs) Handles btn_Reset.Click
        lb_Info.Items.Insert(0, "Cam reset")
        RestartCam()
    End Sub

    Private Sub btn_Analyse_Click(sender As Object, e As EventArgs) Handles btn_Analyse.Click
        ImgageAnalyse()
    End Sub

    'Obj Search
    Private Sub btn_SearchObj_Click(sender As Object, e As EventArgs) Handles btn_SearchObj.Click
        '1. Objektprüfen und Holen
        Dim AktSearch As SearchObj
        Dim AktResult As MyObekt
        Try
            AktSearch = _MySearchObjekte.ElementAt(CInt(num_SearchObj.Value))
        Catch ex As Exception
            Try
                AktSearch = _MySearchObjekte.Last
                num_SearchObj.Value = _MySearchObjekte.IndexOf(_MySearchObjekte.Last) + 1
            Catch ex1 As Exception
                lb_Info.Items.Insert(0, "Keine objekte Gefunden")
                Exit Sub
            End Try
        End Try

        '2. Objektinfos Anzeigen
        lbl_InfoName.Text = AktSearch.Name
        lbl_InfoMaase.Text = AktSearch.Maase

        '3. Bildauswerung starten
        ImgageAnalyse()
        '4. Gefundene Objekte vergleichen 
        'Mase von mm in pixel wandeln
        Dim h As Int32 = CInt(AktSearch.Höhe * num_pixmmH_faktor.Value)
        Dim b As Int32 = CInt(AktSearch.Beite * num_pixmmB_faktor.Value)
        Dim t As Int32 = CInt(AktSearch.Tiefe * num_pixmmH_faktor.Value)
        lb_Info.Items.Insert(0, $"h:{h,4} b:{b,4} t:{t,4}")
        For Each obj As MyObekt In _MyObjekte
            If obj.Passend(h, b, t, CInt(Num_SearchToleranz.Value)) Then
                AktResult = obj
                Exit For
            End If
        Next
        If AktResult Is Nothing Then
            lb_Info.Items.Insert(0, "Es wurde kein passendes Objekt gefunden.")
            Exit Sub
        End If
        '5. Passendes Objekt Anzeigen
        lbl_FoundObj.Text = AktResult.ToString()
        Dim mm As Int32 = CInt(AktResult.Dist_Max() / num_pixmmH_faktor.Value)
        lbl_FoundWidth.Text = $"{AktResult.Dist_Max(),4} pixel = {mm,4} mm"
        lbl_FoundZent.Text = "####"
        lbl_Found_Rot.Text = "###"

        '6. werte senden
        '###
    End Sub

    Private Sub btn_AddSearchObj_Click(sender As Object, e As EventArgs) Handles btn_AddSearchObj.Click
        lb_SearchObjList.Items.Clear()
        Dim tmpObj As New SearchObj(_MySearchObjekte.Count + 1, tb_searchObjName.Text, CInt(num_newSearchObjH.Value), CInt(num_newSearchObjB.Value), CInt(num_newSearchObjT.Value))
        _MySearchObjekte.Add(tmpObj)

        For Each obj As SearchObj In _MySearchObjekte
            lb_SearchObjList.Items.Add(obj.ToString())
        Next
    End Sub

    'TCP_Variablen
    Private Sub btn_TCP_Connect_Click(sender As Object, e As EventArgs) Handles btn_TCP_Connect.Click
        lbl_TCP_Status.ForeColor = Color.Black
        lbl_TCP_Status.Text = "Warten.."
        _TcpVariablen.Connect(tb_TCP_HOST.Text, CInt(num_TCP_Port.Value))
    End Sub
    Private Sub btn_TCPVariable_New_Click(sender As Object, e As EventArgs) Handles btn_TCPVariable_New.Click
        If tb_TCPVarible_Name.Text <> "" Then
            _TcpVariablen.AddVariable(tb_TCPVarible_Name.Text)
            _TcpVariablen.SetVariable(tb_TCPVarible_Name.Text, CInt(num_TCPVariable_Wert.Value))
            _refreshDataGridView()
        End If
    End Sub
    Private Sub btn_TCPVariable_Set_Click(sender As Object, e As EventArgs) Handles btn_TCPVariable_Set.Click
        If _TcpVariablen.Exists(tb_TCPVarible_Name.Text) Then
            _TcpVariablen.SetVariable(tb_TCPVarible_Name.Text, CInt(num_TCPVariable_Wert.Value))
        Else
            lb_Info.Items.Insert(0, $"Variablenfehler: Die Variable:{tb_TCPVarible_Name.Text}  Existiert nicht")
        End If
        _refreshDataGridView()
    End Sub
    Private Sub btn_TCPVariable_Del_Click(sender As Object, e As EventArgs) Handles btn_TCPVariable_Del.Click
        If _TcpVariablen.Exists(tb_TCPVarible_Name.Text) Then
            _TcpVariablen.RemoveVariable(tb_TCPVarible_Name.Text)
        Else
            lb_Info.Items.Insert(0, $"Variablenfehler: Die Variable:{tb_TCPVarible_Name.Text}  Existiert nicht")
        End If
        _refreshDataGridView()
    End Sub

    'Konfig
    Private Sub btn_tiefe_Click(sender As Object, e As EventArgs) Handles btn_tiefe.Click
        Dim MatResurce As New Mat
        If _MatDepth Is Nothing Then
            If _MatRefD IsNot Nothing Then
                If cb_Tiefe_aktMaske.Checked Then
                    MatResurce = Maskieren(_MatRefD, CInt(num_MaskH.Value), CInt(num_MaskV.Value), CInt(num_CamOffset.Value))
                Else
                    MatResurce = _MatRefD
                End If
                Tiefentest(MatResurce, _MatColor)
            Else
                lb_Info.Items.Insert(0, "Es existiert noch kein Tiefenbild, das Analysiert werden kann")
            End If
        Else
            If cb_Tiefe_aktMaske.Checked Then
                MatResurce = Maskieren(_MatDepth, CInt(num_MaskH.Value), CInt(num_MaskV.Value), CInt(num_CamOffset.Value))
            Else
                MatResurce = _MatDepth
            End If
            Tiefentest(MatResurce, _MatColor)
        End If
        _MatColor.CopyTo(_DisColor)
        CvInvoke.Resize(_DisColor, _DisColor, New Size(640, 480))
        ib_res_02.Image = _DisColor.ToImage(Of Bgr, Byte)
        TC2_Bilder.SelectedTab = P2_Result
    End Sub

    Private Sub btn_pos_Click(sender As Object, e As EventArgs) Handles btn_pos.Click
        If _MatColor Is Nothing Then
            If _MatRefC IsNot Nothing Then
                _DisColor = Maskieren(_MatRefC, CInt(num_MaskH.Value), CInt(num_MaskV.Value))
                _DisDepth = Maskieren(_MatRefDc, CInt(num_MaskH.Value), CInt(num_MaskV.Value), CInt(num_CamOffset.Value))
            Else
                lb_Info.Items.Insert(0, "Es existiert noch kein Bild, zum festlegen der Position")
            End If
        Else
            _DisColor = Maskieren(_MatColor, CInt(num_MaskH.Value), CInt(num_MaskV.Value))
            _DisDepth = Maskieren(_MatDepthC, CInt(num_MaskH.Value), CInt(num_MaskV.Value), CInt(num_CamOffset.Value))
        End If
        'Anzeigen
        CvInvoke.Resize(_DisColor, _DisColor, New Size(640, 480))
        ib_res_01.Image = _DisColor.ToImage(Of Bgr, Byte)
        CvInvoke.Resize(_DisDepth, _DisDepth, New Size(640, 480))
        ib_res_02.Image = _DisDepth.ToImage(Of Bgr, Byte)
        TC2_Bilder.SelectedTab = P2_Result
    End Sub

    Private Sub btn_Konf_Col_Click(sender As Object, e As EventArgs) Handles btn_Konf_Col.Click
        KonfigPipe(Enum_Format.color, cWidht, cHeight, Format.Bgr8, CInt(comb_Konf_Col_FPS.SelectedItem))
    End Sub
    Private Sub btn_Konf_Depth_Click(sender As Object, e As EventArgs) Handles btn_Konf_Depth.Click
        KonfigPipe(Enum_Format.depth, cWidht, cHeight, Format.Z16, CInt(comb_Konf_Dep_FPS.SelectedItem))
    End Sub

    Private Sub btn_CamOffset_Reset_Click(sender As Object, e As EventArgs) Handles btn_CamOffset_Reset.Click
        num_CamOffset.Value = cCamOffset
    End Sub

    Private Sub cb_colortaken_CheckedChanged(sender As Object, e As EventArgs)
        ImgStatus()
    End Sub
    Private Sub cb_depthtaken_CheckedChanged(sender As Object, e As EventArgs)
        ImgStatus()
    End Sub
    Private Sub cb_depthcTaken_CheckedChanged(sender As Object, e As EventArgs)
        ImgStatus()
    End Sub
    Private Sub cb_refcTaken_CheckedChanged(sender As Object, e As EventArgs)
        ImgStatus()
    End Sub
    Private Sub cb_refdTaken_CheckedChanged(sender As Object, e As EventArgs)
        ImgStatus()
    End Sub
    Private Sub cb_refdcTaken_CheckedChanged(sender As Object, e As EventArgs)
        ImgStatus()
    End Sub

    'Konfig Farbe
    Private Sub comb_Konf_Col_FPS_SelectedIndexChanged(sender As Object, e As EventArgs) Handles comb_Konf_Col_FPS.SelectedIndexChanged

    End Sub
    Private Sub comb_Konf_Col_Format_SelectedIndexChanged(sender As Object, e As EventArgs) Handles comb_Konf_Col_Format.SelectedIndexChanged

    End Sub
    Private Sub comb_Konf_Col_Auflösung_SelectedIndexChanged(sender As Object, e As EventArgs) Handles comb_Konf_Col_Auflösung.SelectedIndexChanged

    End Sub
    'Konfig Tiefe
    Private Sub comb_Konf_Dep_FPS_SelectedIndexChanged(sender As Object, e As EventArgs) Handles comb_Konf_Dep_FPS.SelectedIndexChanged

    End Sub
    Private Sub comb_Konf_Dep_Format_SelectedIndexChanged(sender As Object, e As EventArgs) Handles comb_Konf_Dep_Format.SelectedIndexChanged

    End Sub
    Private Sub comb_Konf_Dep_Auflösung_SelectedIndexChanged(sender As Object, e As EventArgs) Handles comb_Konf_Dep_Auflösung.SelectedIndexChanged

    End Sub


    'Test
    Private Sub btn_TestVerschieben_Click(sender As Object, e As EventArgs) Handles btn_TestVerschieben.Click
        Dim test As New Mat
        Dim test2 As New Mat
        test = ImageVerschieben(_MatColor, CInt(num_CamOffset.Value))
        Dim v1, v2 As New ImageViewer
        v1.Image = test.Clone : v1.Text = "verschoben"
        v2.Image = _MatDepthC.Clone : v2.Text = " "
        CvInvoke.Imshow("Original", _MatColor)
        lb_Info.Items.Insert(0, $"test rows:{test.Rows} cols:{test.Cols} | depth rows:{_MatDepthC.Rows} cols:{_MatDepthC.Cols}")
        v1.Show()
        v2.Show()
    End Sub

    '-----------------------------------------------------------------------------------------------------------------------
    'Timer
    '-----------------------------------------------------------------------------------------------------------------------
    Private Sub T2_CamBoot_Tick(sender As Object, e As EventArgs) Handles T2_CamBoot.Tick
        T2_CamBoot.Stop()
        'If (TakePicture(_MatRefC, _MatRefD, _MatRefDc)) Then
        '    lb_Info.Items.Insert(0, "Referenzbilder Color erfolgreich")
        '    _MatRefC.CopyTo(_DisRefC)
        '    CvInvoke.Resize(_DisRefC, _DisRefC, New Size(640, 480))
        '    ib_Color.Image = _DisRefC.ToImage(Of Bgr, Byte)
        '    _MatRefDc.CopyTo(_DisRefD)
        '    CvInvoke.Resize(_DisRefD, _DisRefD, New Size(640, 480))
        '    ib_Depth.Image = _DisRefD.ToImage(Of Bgr, Byte)
        'End If
    End Sub

    '-----------------------------------------------------------------------------------------------------------------------
    'Events
    '-----------------------------------------------------------------------------------------------------------------------
    'TCP_Variablen
    Private Sub _TcpVariablen_Connected() Handles _TcpVariablen.Connected
        lbl_TCP_Status.ForeColor = Color.Green
        lbl_TCP_Status.Text = "Verbunden"
    End Sub
    Private Sub _TcpVariablen_ConnectError() Handles _TcpVariablen.ConnectError
        lbl_TCP_Status.ForeColor = Color.Red
        lbl_TCP_Status.Text = "Fehler"
    End Sub
    Private Sub _TcpVariablen_Disconnected() Handles _TcpVariablen.Disconnected
        Me.Invoke(Sub() Disconect())
    End Sub

    Private Sub _TcpVariablen_VariableChanged(name As String, val As Integer) Handles _TcpVariablen.VariableChanged
        Me.Invoke(Sub() VaribleChange(name, val))

    End Sub

    Sub Disconect()
        lbl_TCP_Status.ForeColor = Color.Yellow
        lbl_TCP_Status.Text = "Getrennt"
    End Sub

    Sub VaribleChange(name As String, val As Int32)
        _refreshDataGridView()
        lb_Info.Items.Insert(0, $"Variabel wurde geändert {name} {val}")
    End Sub

    '-----------------------------------------------------------------------------------------------------------------------
    'Sonstige Sub's
    '-----------------------------------------------------------------------------------------------------------------------
    Private Sub KonfigPipe(typ As Enum_Format, widht As Int32, height As Int32, format As Format, fps As Int32)

        _MyCFG.DisableAllStreams()
        Try
            'Color Konfig
            If typ = Enum_Format.color Then
                _MyCFG.EnableStream(Stream.Color, widht, height, format, fps)
                lb_Info.Items.Insert(0, "RGB_Konfig added")
            End If

            'Depth Konfig
            If typ = Enum_Format.depth Then
                _MyCFG.EnableStream(Stream.Depth, widht, height, format, fps)
                lb_Info.Items.Insert(0, "Depth_Konfig added")
            End If

            RestartCam()
        Catch ex As Exception
            lb_Info.Items.Insert(0, $"Konfigurations oder Pipeline Fehle: {ex.Message}")
            Return
        End Try
    End Sub
    Private Sub KonfigPipe()

        _MyCFG.DisableAllStreams()
        Try
            'Color Konfig

            _MyCFG.EnableStream(Stream.Color, cWidht, cHeight, Format.Bgr8, cfpsColor)
            lb_Info.Items.Insert(0, "RGB_Konfig added")
            'Depth Konfig

            _MyCFG.EnableStream(Stream.Depth, cWidht, cHeight, Format.Z16, cfpsDepth)
            lb_Info.Items.Insert(0, "Depth_Konfig added")

            RestartCam()
        Catch ex As Exception
            lb_Info.Items.Insert(0, $"Konfigurations oder Pipeline Fehle: {ex.Message}")
            Return
        End Try
    End Sub

    Private Sub RestartCam()
        If _MyPipelineAktiv Then
            _MyPipelineAktiv = False
            btn_NewImg.Enabled = False
            btn_RefImg.Enabled = False
            _MyPipeline.Stop()
            '_MyPipeline.Release()
            '_MyPipeline.Dispose()
        End If
        Thread.Sleep(50)
        Try
            _MyPipeline.Start(_MyCFG)
            _MyPipelineAktiv = True
            T2_CamBoot.Start()
            lb_Info.Items.Insert(0, $"Cam_Boot")
        Catch ex As Exception
            lb_Info.Items.Insert(0, $"Konfigurations oder Pipeline Fehle: {ex.Message}")
            _MyPipelineAktiv = False
        End Try
        Thread.Sleep(50)
        btn_NewImg.Enabled = True
        btn_RefImg.Enabled = True
        lb_Info.Items.Insert(0, $"Cam_Ready")
    End Sub

    Private Sub ImgStatus()
        If (_MatColor IsNot Nothing) Then
            _ColorImgTaken = (Not _MatColor.IsEmpty)
        End If
        If (_MatDepth IsNot Nothing) Then
            _DepthImgTaken = (Not _MatDepth.IsEmpty)
        End If
        If (_MatDepthC IsNot Nothing) Then
            _DepthCImgTaken = (Not _MatDepthC.IsEmpty)
        End If
        If (_MatRefC IsNot Nothing) Then
            _RefCImgTaken = (Not _MatRefC.IsEmpty)
        End If
        If (_MatRefD IsNot Nothing) Then
            _RefDImgTaken = (Not _MatRefD.IsEmpty)
        End If
        cb_colortaken.Checked = _ColorImgTaken
        cb_depthtaken.Checked = _DepthImgTaken
        cb_depthcTaken.Checked = _DepthCImgTaken
        cb_refcTaken.Checked = _RefCImgTaken
        cb_refdcTaken.Checked = _RefDImgTaken
        cb_refdTaken.Checked = _RefDImgTaken
        btn_tiefe.Enabled = _DepthImgTaken Or _RefDImgTaken
        btn_pos.Enabled = _ColorImgTaken Or _RefCImgTaken
    End Sub

    Private Sub Autoreferenz()
        'For i = 1 To 10  ' Möglicherweise machen hier 30 Frames Sinn
        '    Using frames = _MyPipeline.WaitForFrames()
        '    End Using
        'Next
        T2_CamBoot.Start()
    End Sub

    Private oldPic As New Mat

    Private Shared Sub DetectObject(detectionFrame As Mat, displayFrame As Mat)

        Using contours As VectorOfVectorOfPoint = New VectorOfVectorOfPoint()
            '-------------------------------
            Dim testMat As New Mat
            '--------------------------------
            CvInvoke.FindContours(detectionFrame, contours, Nothing, RetrType.List, ChainApproxMethod.ChainApproxSimple)
            If contours.Size > 0 Then
                Form_Main.lb_Info.Items.Insert(0, $"{contours.Size} Konturen erkannt")
                Dim IFL As New List(Of IndexFläche)
                For j As Int32 = 0 To contours.Size - 1
                    CvInvoke.Circle(displayFrame, contours(j).Item(0), 5, New MCvScalar(0, 255, 0), 3)
                    'CvInvoke.Circle(displayFrame, contours(j).Item(contours(j).Size), 5, New MCvScalar(0, 255, 0))
                Next
                displayFrame.CopyTo(testMat)
                CvInvoke.Imshow("test", testMat)
                '    
                'Next
                For i As Integer = 0 To contours.Size - 1
                    Dim contour As VectorOfPoint = contours(i)
                    Dim area As Double = CvInvoke.ContourArea(contour)

                    IFL.Add(New IndexFläche(i, area))
                Next

                Dim Sortiert = From el In IFL Order By el.fläche Descending
                Form_Main.lb_Info.Items.Insert(0, $"{Sortiert.Count} Konturen sortiert")
                'For i = 0 To System.Math.Min(Sortiert.Count - 1, 2)
                '    MarkDetectedObject(displayFrame, contours(Sortiert(i).index), Sortiert(i).fläche)
                'Next
                'For i = 0 To Sortiert.Count - 1
                '    MarkDetectedObject(displayFrame, contours(Sortiert(i).index), Sortiert(i).fläche)
                'Next
                'CvInvoke.Imshow("Final", displayFrame)
            End If
        End Using
    End Sub

    Private Shared drawingColor As MCvScalar = New Bgr(Color.Red).MCvScalar

    Private Shared Sub MarkDetectedObject(ByVal frame As Mat, ByVal contour As VectorOfPoint, ByVal area As Double)
        Dim box As Rectangle = CvInvoke.BoundingRectangle(contour)
        CvInvoke.Polylines(frame, contour, True, drawingColor)
        CvInvoke.Rectangle(frame, box, drawingColor)
        Dim center As Point = New Point(box.X + box.Width \ 2, box.Y + box.Height \ 2)
        Dim info = New String() {$"Area: {area}", $"Position: {center.X}, {center.Y}"}
    End Sub

    Private Sub ImgOfInterest_Tiefe(resurce As Mat, result As Mat, MinDepth As Double, MaxDepth As Double)
        Dim dst As New Mat
        CvInvoke.Threshold(resurce, dst, MinDepth, 100, ThresholdType.ToZero)
        CvInvoke.Threshold(dst, result, MaxDepth, 100, ThresholdType.ToZeroInv)
    End Sub

    Private Sub ImgOfInterest_Rev(resurce As Mat, result As Mat, rev As Mat)
        CvInvoke.AbsDiff(resurce, rev, result)
    End Sub

    Private Sub Tiefentest(resurce As Mat, result As Mat)
        Dim PointMin As MyPoint
        Dim PointMax As MyPoint
        Dim wert As Int16
        wert = UmwandlungClass.GetInt16Value(resurce, 0, 0)
        PointMax = New MyPoint(0, 0, 0)
        PointMin = New MyPoint(0, 0, Int16.MaxValue)
        For Zeile = 1 To resurce.Rows - 1
            For Spalte = 1 To resurce.Cols - 1
                wert = UmwandlungClass.GetInt16Value(resurce, Zeile, Spalte)
                If wert <> 0 Then
                    If PointMax.Z < wert Then
                        PointMax.X = Spalte
                        PointMax.Y = Zeile
                        PointMax.Z = wert
                    End If
                    If PointMin.Z > wert Then
                        PointMin.X = Spalte
                        PointMin.Y = Zeile
                        PointMin.Z = wert
                    End If
                End If
            Next
        Next
        lbl_pointMax.Text = PointMax.ToString
        lbl_pointMin.Text = PointMin.ToString
        CvInvoke.Circle(result, New Point(PointMin.X, PointMin.Y), 6, New MCvScalar(255, 255, 255), 3)
        CvInvoke.Circle(result, New Point(PointMin.X, PointMin.Y), 1, New MCvScalar(0, 255, 0), 1)
        CvInvoke.Circle(result, New Point(PointMax.X, PointMax.Y), 6, New MCvScalar(255, 255, 255), 3)
        CvInvoke.Circle(result, New Point(PointMax.X, PointMax.Y), 1, New MCvScalar(255, 0, 0), 1)
    End Sub

    Private Sub _refreshDataGridView()
        dgv_TCPVariableViewer.DataSource = _TcpVariablen.GetGridViewDataSource
    End Sub
    '-----------------------------------------------------------------------------------------------------------------------
    'Sonstige Funktionen
    '-----------------------------------------------------------------------------------------------------------------------
    Private Function TakePicture(ByRef color As Mat, ByRef depth As Mat, ByRef depthc As Mat) As Boolean
        Dim Mydata As FrameSet
        lb_Info.Items.Insert(0, $"---------------------------------")
        lb_Info.Items.Insert(0, $"TakePicture: Neues Bild")

        'Bildvorhanden zurücksetzen

        If _MyPipeline.PollForFrames(Mydata) Then
            If Mydata IsNot Nothing Then
                color = ToMat(Mydata.ColorFrame).Clone
                depth = ToMat(Mydata.DepthFrame, DepthType.Cv16S).Clone
                depthc = ToMat(_MyColorMap.Colorize(Mydata.DepthFrame)).Clone
            Else
                lb_Info.Items.Insert(0, "TakePicture: Fehler PollForFrames gab keine Daten zurück")
            End If
        Else
            lb_Info.Items.Insert(0, $"TakePicture: Fehler bei PollForFrames [{_trys,2}]")
            If _trys < cMaxTrys Then
                _trys += 1
                Return False
            Else
                _trys = 0
                lb_Info.Items.Insert(0, "TakePicture: Auto Restart")
                RestartCam()
                Return False
            End If
        End If
        If _ColorImgTaken And _DepthImgTaken Then
            btn_Analyse.Enabled = True
        End If
        ImgStatus()
        Return True
    End Function

    Private Function ToMat(Source As VideoFrame, Optional Depth As DepthType = DepthType.Cv8U) As Mat
        Dim NrChanels As Int32
        If Depth = DepthType.Cv16S Or Depth = DepthType.Cv16U Then
            NrChanels = Source.BitsPerPixel \ 16
        ElseIf Depth = DepthType.Cv8S Or Depth = DepthType.Cv8U Then
            NrChanels = Source.BitsPerPixel \ 8
        Else
            NrChanels = Source.BitsPerPixel \ 8
        End If

        Return New Mat(New Size(cWidht, cHeight), Depth, NrChanels, Source.Data, Source.Stride)
    End Function

    Private Function ImgageAnalyse() As Boolean
        'Alte Objekte löschen
        LB_obj.Items.Clear()
        _MyObjekte.Clear()
        Dim vortsetzen As Boolean = False
        Dim Ergebnis As Boolean = False
        If Not _ColorImgTaken And Not _DepthImgTaken Then
            vortsetzen = TakePicture(_MatColor, _MatDepth, _MatDepthC)
            If vortsetzen Then
                CvInvoke.Resize(_MatColor, _DisColor, New Size(640, 480))
                ib_new_Color.Image = _DisColor
                CvInvoke.Resize(_MatDepthC, _DisDepthC, New Size(640, 480))
                ib_new_Depth.Image = _DisDepthC
            End If
        Else
            vortsetzen = True
        End If

        If vortsetzen Then
            If (cb_ioi_Differenz.Checked And _RefCImgTaken And _RefDImgTaken) Or Not cb_ioi_Differenz.Checked Then
                If (cb_ioi_Depth.Checked And _DepthImgTaken) Or Not cb_ioi_Depth.Checked Then
                    If rb_Auswertung_Color.Checked Then
                        Ergebnis = MyWatershedColor(_MatColor, _MatResult, _MatRefC, cb_debug.Checked)
                    End If
                    If rb_Auswertung_Depth.Checked Then
                        Ergebnis = MyWatershedDepth(_MatDepth, _MatResult, _MatRefD, cb_debug.Checked)
                    End If
                    If rb_Auswertung_Kombi.Checked Then

                    End If
                Else
                    lb_Info.Items.Insert(0, "Für Tiefen Filderung wird ein Tiefenbild benötigt")
                    Return False
                End If
            Else
                lb_Info.Items.Insert(0, "Für Differenz Erkennung wird ein Reverenzbild benötigt")
                Return False
            End If
            If Ergebnis Then
                CvInvoke.Resize(_MatResult, _DisRes01, New Size(640, 480))
                ib_res_01.Image = _DisRes01.ToImage(Of Bgr, Byte)
                'CvInvoke.Resize(_DisDepth, _DisDepth, New Size(640, 480))
                'ib_Depth.Image = _DisDepth.ToImage(Of Bgr, Byte)
                'ib_Color.Image = _MatDepth.ToImage(Of Gray, Byte)
                'ib_Depth.Image = _MatDif.ToImage(Of Bgr, Byte)
                TC2_Bilder.SelectedTab = P2_Result
            End If
            Return Ergebnis
        Else
            lb_Info.Items.Insert(0, "Es wurden keine Bilder gefunden")
            Return False
        End If
    End Function

    Private Function Maskieren(resurce As Mat, maskH As Int32, maskV As Int32, Optional offset As Int32 = 0) As Mat
        Dim Maske As Mat
        Dim Ergebnis As New Mat
        Dim MIn As Byte() = {CByte(1), CByte(1), CByte(1)}
        Dim MOut As Byte() = {CByte(0), CByte(0), CByte(0)}
        If resurce.NumberOfChannels = 3 Then
            Maske = New Mat(New Size(resurce.Width, resurce.Height), DepthType.Cv8U, 3)

        Else
            Maske = New Mat(New Size(cWidht, cHeight), DepthType.Cv16S, 1)

        End If
        Dim UperBord As Int32 = maskH '135
        Dim LowerBord As Int32 = Maske.Height - maskH '720-135 = 585
        Dim LeftBord As Int32 = maskV - offset '305
        Dim RightBord As Int32 = Maske.Width - maskV - offset '1280-305 = 975
        lb_Info.Items.Insert(0, $"Lower Bord: {LowerBord} | Right Bord: {RightBord}")
        For Zeile = 0 To Maske.Rows
            For Spalte = 0 To Maske.Cols
                'If (Zeile <= LeftBord Or Zeile >= RightBord) And (Spalte <= UperBord Or Spalte >= LowerBord) Then
                If (Zeile > UperBord And Zeile < LowerBord) And (Spalte > LeftBord And Spalte < RightBord) Then
                    UmwandlungClass.SetByteValues(Maske, Zeile, Spalte, MIn)
                Else
                    UmwandlungClass.SetByteValues(Maske, Zeile, Spalte, MOut)
                End If
            Next
        Next
        TC2_Bilder.TabPages(3).Text = "Maske"
        ib_depth_01.Image = Maske
        CvInvoke.Multiply(resurce, Maske, Ergebnis)
        Return Ergebnis
    End Function

    Private Function ImageVerschieben(resurce As Mat, offset As Int32) As Mat
        Dim Verschoben As New Mat(resurce.Size, DepthType.Cv8U, 3)
        Verschoben.SetTo(New MCvScalar(0)) ' AnzeigenBild auf schwarz setzen
        For Zeile = 0 To Verschoben.Rows - 1
            For Spalte = 0 To Verschoben.Cols - 1
                If Spalte <= (Verschoben.Cols - (offset + 2)) Then
                    Dim value As Byte() = UmwandlungClass.GetByteValue(resurce, Zeile, Spalte + offset)
                    UmwandlungClass.SetByteValues(Verschoben, Zeile, Spalte, value)
                End If
            Next
        Next
        Return Verschoben
    End Function

    Private Function MyWatershedColor(resurce As Mat, result As Mat, rev As Mat, Optional debug As Boolean = False) As Boolean
        Dim ImgOfInterest As New Mat
        Dim Laplace32F As Mat
        Dim Result32F As Mat
        Dim IMG32F As Mat
        Dim Laplace8U As Mat
        Dim Result8U As Mat
        Dim GrayImg As New Mat
        Dim BinaerImg As New Mat
        Dim DistImg As New Mat
        Dim DistImgClear As New Mat
        Dim DistImg8u As New Mat
        Dim Markers As New Mat
        Dim Mask As New Mat
        Dim TmpResult As New Mat
        Dim dst As New Mat

        Const cThreshhold1 = 10
        Const cThreshhold2 = 0.3


        '1. Hintergrung Ausblenden entweder Mit Referenzbild oder über Tiefe
        ' Über Tiefe und Treshold
        'ImgOfInterest_Tiefe(resurce, ImgOfInterest, 0.25, 1.25) 'Tiefe zwischen 25cm und 1.25m
        'ImgOfInterest_Tiefe(resurce, ImgOfInterest, 5600, 7300) 'Tiefe zwischen 29cm und 69cm
        ' Über Reverenzbild
        ImgOfInterest_Rev(resurce, ImgOfInterest, rev)
        '########
        'CvInvoke.CvtColor(ImgOfInterest, ImgOfInterest, ColorConversion.Bgr2Gray, 1)
        'CvInvoke.Threshold(ImgOfInterest, ImgOfInterest, 100, 255, ThresholdType.ToZero)
        ''CvInvoke.GaussianBlur(picture, gausPic, New Drawing.Size(3, 3), 2)
        'CvInvoke.Erode(ImgOfInterest, ImgOfInterest, Nothing, New Point(-1, -1), 3, BorderType.Default, New MCvScalar(1))
        'CvInvoke.Dilate(ImgOfInterest, ImgOfInterest, Nothing, New Point(-1, -1), 3, BorderType.Default, New MCvScalar(1))

        If debug Then
            CvInvoke.Imshow("ImgOfInterest", ImgOfInterest)
        End If

        '2. Bild Aufbereiten (laplace)
        Dim Kernel As New Matrix(Of Single)(New Single(,) {
                                            {1, 1, 1},
                                            {1, -8, 1},
                                            {1, 1, 1}})
        Laplace32F = New Mat(ImgOfInterest.Size, DepthType.Cv32F, ImgOfInterest.NumberOfChannels)
        Result32F = New Mat(ImgOfInterest.Size, DepthType.Cv32F, ImgOfInterest.NumberOfChannels)
        CvInvoke.Filter2D(ImgOfInterest, Laplace32F, Kernel, New Point(-1, -1))
        IMG32F = New Mat : ImgOfInterest.ConvertTo(IMG32F, DepthType.Cv32F)
        CvInvoke.Subtract(IMG32F, Laplace32F, Result32F)
        'zurückwandeln
        Laplace8U = New Mat
        Result8U = New Mat
        Laplace32F.ConvertTo(Laplace8U, DepthType.Cv8U)
        Result32F.ConvertTo(Result8U, DepthType.Cv8U)
        If debug Then
            CvInvoke.Imshow("Result32", Result32F)
            CvInvoke.Imshow("Result8", Result8U)
        End If
        CvInvoke.GaussianBlur(Result8U, Result8U, New Drawing.Size(5, 5), 3)

        '3. Graustufen- & Binäresbild erzeugen
        If Result8U.NumberOfChannels = 3 Then
            CvInvoke.CvtColor(Result8U, GrayImg, ColorConversion.Bgr2Gray, 1)
        Else
            Result8U.CopyTo(GrayImg)
        End If
        CvInvoke.Threshold(GrayImg, BinaerImg, cThreshhold1, 255, ThresholdType.Binary Or ThresholdType.Otsu)
        If debug Then
            CvInvoke.Imshow("BinaerImg", BinaerImg)
        End If



        '4. Distanc Funktion & normalize
        CvInvoke.DistanceTransform(BinaerImg, DistImg, Nothing, DistType.L2, 3)
        CvInvoke.Normalize(DistImg, DistImg, 0, 1.0, NormType.MinMax)
        If debug Then
            CvInvoke.Imshow("DistImg", DistImg)
        End If

        '5. Bild bereinigen (mit Treshold) und in CV8 wandeln
        CvInvoke.Threshold(DistImg, DistImgClear, cThreshhold2, 1.0, ThresholdType.Binary)

        Dim kernel1 = Mat.Ones(3, 3, DepthType.Cv8U, 1)
        CvInvoke.Dilate(DistImgClear, DistImgClear, kernel1, New Point(-1, -1), 1, BorderType.Default, New MCvScalar(0))

        If debug Then
            CvInvoke.Imshow("DistImgClear", DistImgClear)
        End If

        CvInvoke.Normalize(DistImgClear, DistImg8u, 0, 255, NormType.MinMax, DepthType.Cv8U)

        '6. Objekte bezeichnen
        Dim Contours As New VectorOfVectorOfPoint
        CvInvoke.FindContours(DistImg8u, Contours, Nothing, RetrType.List, ChainApproxMethod.ChainApproxSimple)
        lb_Info.Items.Insert(0, $"Es wurden {Contours.Size} Kontuen Gefunden")
        ' Create the marker image for the watershed algorithm
        Markers = New Mat(resurce.Size, DepthType.Cv32S, 1) ' Zur Anzeige farbiger Bilder
        Markers.SetTo(New MCvScalar(0)) ' AnzeigenBild auf schwarz setzen
        For i = 0 To Contours.Size - 1
            Dim Colo = New MCvScalar((i + 1) * 10) ' An 1 nummerieren. 0 ist ja der Hintergrund
            CvInvoke.DrawContours(Markers, Contours, i, Colo, -1) ' -1 damit die contour ausgefüllt wird
        Next
        If debug Then
            Dim Maskshow As New Mat
            Markers.ConvertTo(Maskshow, DepthType.Cv8U)
            CvInvoke.Imshow("markers32S1", Maskshow)
        End If

        CvInvoke.Circle(Markers, New Point(5, 5), 3, New MCvScalar(255), -1)
        If debug Then
            Dim Maskshow2 As New Mat
            Markers.ConvertTo(Maskshow2, DepthType.Cv8U)
            CvInvoke.Imshow("markers32S1_2", Maskshow2)
        End If

        '7. Watershed zu objekt Auswertung  Unklar ob Result8U oder DistImg8u
        Dim imgWater8U3 As New Mat
        If Result8U.NumberOfChannels = 3 Then
            imgWater8U3 = Result8U
        Else
            CvInvoke.CvtColor(Result8U, imgWater8U3, ColorConversion.Gray2Bgr, 3)
        End If
        ' CvInvoke.WaitKey(0)
        CvInvoke.Watershed(imgWater8U3, Markers)

        '8. Farbenvergeben
        Dim Nc = Contours.Size ' Anzahl Contours = Anzahl an markierten Elementen
        Dim Cols(Nc - 1) As MCvScalar : Dim RND As New Random
        For i = 0 To Nc - 1
            Cols(i) = New Bgr(RND.Next(0, 256), RND.Next(0, 256), RND.Next(0, 256)).MCvScalar
        Next

        Dim ZeichenMat2 = Mat.Zeros(Markers.Rows, Markers.Cols, DepthType.Cv8U, 3)
        For Zeile = 0 To Markers.Rows - 1
            For Spalte = 0 To Markers.Cols - 1
                Dim wert As Int32
                Dim gefunden As Boolean = False
                wert = UmwandlungClass.GetInt32Value(Markers, Zeile, Spalte)
                If _MyObjekte.Count < 1 And wert > 0 Then
                    Dim tmpObjekt As New MyObekt(wert)
                    Dim tiefe As Int32 = UmwandlungClass.GetInt32Value(_MatDepth, Zeile, Spalte)
                    tmpObjekt.Add_Ref(Zeile, Spalte, tiefe)
                    _MyObjekte.Add(tmpObjekt)
                Else
                    For Each ob As MyObekt In _MyObjekte
                        If wert = ob.ID Then
                            gefunden = True
                            Dim tiefe As Int32 = UmwandlungClass.GetInt32Value(_MatDepth, Zeile, Spalte)
                            ob.Add_Ref(Zeile, Spalte, tiefe)
                        End If
                    Next
                    If Not gefunden And wert > 0 Then
                        Dim tmpObjekt As New MyObekt(wert)
                        Dim tiefe As Int32 = UmwandlungClass.GetInt32Value(_MatDepth, Zeile, Spalte)
                        tmpObjekt.Add_Ref(Zeile, Spalte, tiefe)
                        _MyObjekte.Add(tmpObjekt)
                    End If
                End If
                Dim Marke As Int32 = UmwandlungClass.GetInt32Value(Markers, Zeile, Spalte)
                If Marke > 0 And Marke < Nc Then
                    Dim ByteWerte() As Byte = {CByte(Cols(Marke).V0), CByte(Cols(Marke).V1), CByte(Cols(Marke).V2)}
                    UmwandlungClass.SetByteValues(ZeichenMat2, Zeile, Spalte, ByteWerte)
                End If
            Next
        Next
        For Each ob2 As MyObekt In _MyObjekte
            LB_obj.Items.Add(ob2.ToString)
        Next
        ZeichenMat2.CopyTo(result)
        If debug Then
            CvInvoke.Imshow("Ergebnis", ZeichenMat2)
        End If

        Return True
    End Function

    Private Function MyWatershedDepth(resurce As Mat, result As Mat, rev As Mat, Optional debug As Boolean = False) As Boolean
        Dim ImgOfInterest As New Mat
        Dim Laplace32F As Mat
        Dim Result32F As Mat
        Dim IMG32F As Mat
        Dim Laplace16s As Mat
        Dim Result16s As Mat
        Dim Result8U As New Mat
        Dim GrayImg As New Mat
        Dim BinaerImg As New Mat
        Dim DistImg As New Mat
        Dim DistImgClear As New Mat
        Dim DistImg8u As New Mat
        Dim Markers As New Mat
        Dim Mask As New Mat
        Dim TmpResult As New Mat
        Dim dst As New Mat
        Dim Display As New Mat

        Const cThreshhold1 = 10
        Const cThreshhold2 = 0.1 '0.3



        '1. Hintergrung Ausblenden über Tiefe dann Kiste Ausmaskieren
        ' Über Tiefe und Treshold
        ImgOfInterest_Tiefe(resurce, ImgOfInterest, CInt(num_ThreshHoch.Value), CInt(num_ThreshTief.Value))
        'Maske
        ImgOfInterest = Maskieren(ImgOfInterest, CInt(num_MaskH.Value), CInt(num_MaskV.Value), CInt(num_CamOffset.Value))
        If debug Then
            ImgOfInterest.ConvertTo(Display, DepthType.Cv8U)
            Dim v2 As New ImageViewer
            v2.Image = Display.Clone : v2.Text = "ImgOfInterest"
            v2.Show()
        End If


        '2. Bild Aufbereiten (laplace)
        Dim Kernel As New Matrix(Of Single)(New Single(,) {
                                            {1, 1, 1},
                                            {1, -8, 1},
                                            {1, 1, 1}})
        Laplace32F = New Mat(ImgOfInterest.Size, DepthType.Cv32F, ImgOfInterest.NumberOfChannels)
        Result32F = New Mat(ImgOfInterest.Size, DepthType.Cv32F, ImgOfInterest.NumberOfChannels)
        CvInvoke.Filter2D(ImgOfInterest, Laplace32F, Kernel, New Point(-1, -1))
        IMG32F = New Mat : ImgOfInterest.ConvertTo(IMG32F, DepthType.Cv32F)
        CvInvoke.Subtract(IMG32F, Laplace32F, Result32F)
        'zurückwandeln
        Laplace16s = New Mat
        Result16s = New Mat
        Laplace32F.ConvertTo(Laplace16s, DepthType.Cv16S)
        Result32F.ConvertTo(Result16s, DepthType.Cv16S)
        Result32F.ConvertTo(Result8U, DepthType.Cv8U)
        If debug Then
            CvInvoke.Imshow("Result32", Result32F)
            CvInvoke.Imshow("Result16", Result16s)
            CvInvoke.Imshow("Result8", Result8U)
        End If
        CvInvoke.GaussianBlur(Result16s, Result16s, New Drawing.Size(5, 5), 3)


        '3. Graustufen- & Binäresbild erzeugen
        If Result16s.NumberOfChannels = 3 Then
            CvInvoke.CvtColor(Result16s, GrayImg, ColorConversion.Bgr2Gray, 1)
        Else
            Result16s.CopyTo(GrayImg)
        End If
        GrayImg.ConvertTo(Result8U, DepthType.Cv8U)

        CvInvoke.Threshold(Result8U, BinaerImg, cThreshhold1, 255, ThresholdType.Binary Or ThresholdType.Otsu) 'benötigt cv8u
        If debug Then
            CvInvoke.Imshow("Result8", Result8U)
            CvInvoke.Imshow("BinaerImg", BinaerImg)
        End If


        '4. Distanc Funktion & normalize
        CvInvoke.DistanceTransform(BinaerImg, DistImg, Nothing, DistType.L2, 3) 'Mask size muss 0 || 3 ||5 sein  'CvInvoke.DistanceTransform(BinaerImg, DistImg, Nothing, DistType.L2, 3)
        CvInvoke.Normalize(DistImg, DistImg, 0, 1.0, NormType.MinMax)
        If debug Then
            CvInvoke.Imshow("DistImg", DistImg)
        End If


        '5. Bild bereinigen (mit Treshold) und in CV8 wandeln
        CvInvoke.Threshold(DistImg, DistImgClear, cThreshhold2, 1.0, ThresholdType.Binary) '0.3

        Dim kernel1 = Mat.Ones(3, 3, DepthType.Cv8U, 1)
        CvInvoke.Dilate(DistImgClear, DistImgClear, kernel1, New Point(-1, -1), 1, BorderType.Default, New MCvScalar(0))
        If debug Then
            CvInvoke.Imshow("DistImgClear", DistImgClear)
        End If

        CvInvoke.Normalize(DistImgClear, DistImg8u, 0, 255, NormType.MinMax, DepthType.Cv8U)

        '6. Objekte bezeichnen
        Dim Contours As New VectorOfVectorOfPoint
        CvInvoke.FindContours(DistImg8u, Contours, Nothing, RetrType.List, ChainApproxMethod.ChainApproxSimple)
        lb_Info.Items.Insert(0, $"Es wurden {Contours.Size} Kontuen Gefunden")
        ' Create the marker image for the watershed algorithm
        Markers = New Mat(resurce.Size, DepthType.Cv32S, 1) ' Zur Anzeige farbiger Bilder
        Markers.SetTo(New MCvScalar(0)) ' AnzeigenBild auf schwarz setzen
        For i = 0 To Contours.Size - 1
            Dim Colo = New MCvScalar((i + 1) * 10) ' An 1 nummerieren. 0 ist ja der Hintergrund
            CvInvoke.DrawContours(Markers, Contours, i, Colo, -1) ' -1 damit die contour ausgefüllt wird
        Next
        If debug Then
            Dim Maskshow As New Mat
            Markers.ConvertTo(Maskshow, DepthType.Cv8U)
            CvInvoke.Imshow("markers32S1", Maskshow)
        End If

        CvInvoke.Circle(Markers, New Point(5, 5), 3, New MCvScalar(255), -1)
        If debug Then
            Dim Maskshow2 As New Mat
            Markers.ConvertTo(Maskshow2, DepthType.Cv8U)
            CvInvoke.Imshow("markers32S1_2", Maskshow2)
        End If


        '7. Watershed zu objekt Auswertung  Unklar ob Result8U oder DistImg8u
        Dim imgWater8U3 As New Mat
        If Result8U.NumberOfChannels = 3 Then

            imgWater8U3 = Result8U
        Else
            CvInvoke.CvtColor(Result8U, imgWater8U3, ColorConversion.Gray2Bgr, 3)
        End If

        ' CvInvoke.WaitKey(0)
        CvInvoke.Watershed(imgWater8U3, Markers)
        Dim Maskshow3 As New Mat
        Markers.ConvertTo(Maskshow3, DepthType.Cv8U)
        CvInvoke.Imshow("Watershed", Maskshow3)


        '8. Farbenvergeben
        Dim Nc = Contours.Size ' Anzahl Contours = Anzahl an markierten Elementen
        Dim Cols(Nc) As MCvScalar : Dim RND As New Random 'Dim Cols(Nc - 1) As MCvScalar : Dim RND As New Random
        For i = 0 To Nc '- 1
            Cols(i) = New Bgr(RND.Next(0, 256), RND.Next(0, 256), RND.Next(0, 256)).MCvScalar
        Next

        Dim ZeichenMat2 = Mat.Zeros(Markers.Rows, Markers.Cols, DepthType.Cv8U, 3)
        For Zeile = 0 To Markers.Rows - 1
            For Spalte = 0 To Markers.Cols - 1
                Dim wert As Int32
                Dim gefunden As Boolean = False
                wert = UmwandlungClass.GetInt32Value(Markers, Zeile, Spalte)
                If _MyObjekte.Count < 1 And wert > 0 And wert < 255 Then
                    Dim tmpObjekt As New MyObekt(wert)
                    Dim tiefe As Int32 = UmwandlungClass.GetInt16Value(_MatDepth, Zeile, Spalte)
                    tmpObjekt.Add_Ref(Spalte, Zeile, tiefe)
                    _MyObjekte.Add(tmpObjekt)
                Else
                    For Each ob As MyObekt In _MyObjekte
                        If wert = ob.ID Then
                            gefunden = True
                            Dim tiefe As Int32 = UmwandlungClass.GetInt16Value(_MatDepth, Zeile, Spalte)
                            ob.Add_Ref(Spalte, Zeile, tiefe)
                        End If
                    Next
                    If Not gefunden And wert > 0 And wert < 255 Then
                        Dim tmpObjekt As New MyObekt(wert)
                        Dim tiefe As Int32 = UmwandlungClass.GetInt16Value(_MatDepth, Zeile, Spalte)
                        tmpObjekt.Add_Ref(Spalte, Zeile, tiefe)
                        _MyObjekte.Add(tmpObjekt)
                    End If
                End If
                Dim Marke As Int32 = UmwandlungClass.GetInt32Value(Markers, Zeile, Spalte)
                If Marke > 0 And Marke <= Nc * 10 Then
                    Dim ByteWerte() As Byte = {CByte(Cols(Marke \ 10).V0), CByte(Cols(Marke \ 10).V1), CByte(Cols(Marke \ 10).V2)}
                    UmwandlungClass.SetByteValues(ZeichenMat2, Zeile, Spalte, ByteWerte)
                End If
            Next
        Next

        For Each ob2 As MyObekt In _MyObjekte
            LB_obj.Items.Add(ob2.ToString)
        Next
        ZeichenMat2.CopyTo(result)
        If debug Then
            CvInvoke.Imshow("Ergebnis", ZeichenMat2)
        End If

        '9. Draw Points
        If cb_DrawPoint.Checked Then
            For Each obj As MyObekt In _MyObjekte
                'X min => Grün
                CvInvoke.Circle(ZeichenMat2, New Point(obj.Min_X.X, obj.Min_X.Y), 6, New MCvScalar(255, 255, 255), 3)
                CvInvoke.Circle(ZeichenMat2, New Point(obj.Min_X.X, obj.Min_X.Y), 1, New MCvScalar(0, 255, 0), 1) 'Gün
                'X max => GrünRot
                CvInvoke.Circle(ZeichenMat2, New Point(obj.Max_X.X, obj.Max_X.Y), 6, New MCvScalar(255, 255, 255), 3)
                CvInvoke.Circle(ZeichenMat2, New Point(obj.Max_X.X, obj.Max_X.Y), 1, New MCvScalar(0, 255, 255), 1) 'Gün
                'Y min => Blau
                CvInvoke.Circle(ZeichenMat2, New Point(obj.Min_Y.X, obj.Min_Y.Y), 6, New MCvScalar(255, 255, 255), 3)
                CvInvoke.Circle(ZeichenMat2, New Point(obj.Min_Y.X, obj.Min_Y.Y), 1, New MCvScalar(255, 0, 0), 1) 'Blau
                'Y max => BlauRot
                CvInvoke.Circle(ZeichenMat2, New Point(obj.Max_Y.X, obj.Max_Y.Y), 6, New MCvScalar(255, 255, 255), 3)
                CvInvoke.Circle(ZeichenMat2, New Point(obj.Max_Y.X, obj.Max_Y.Y), 1, New MCvScalar(255, 0, 255), 1) 'Blau
                'Virtueller Start => Rot
                CvInvoke.Circle(ZeichenMat2, New Point(obj.Min_X.X, obj.Min_Y.Y), 6, New MCvScalar(255, 255, 255), 3)
                CvInvoke.Circle(ZeichenMat2, New Point(obj.Min_X.X, obj.Min_Y.Y), 1, New MCvScalar(0, 0, 255), 1) 'Rot
                'Virtuelles Ende => Rotgrünblau
                CvInvoke.Circle(ZeichenMat2, New Point(obj.Max_X.X, obj.Max_Y.Y), 6, New MCvScalar(255, 255, 255), 3)
                CvInvoke.Circle(ZeichenMat2, New Point(obj.Max_X.X, obj.Max_Y.Y), 1, New MCvScalar(125, 125, 255), 1) 'Rot
            Next
            Dim v4 As New ImageViewer
            v4.Image = ZeichenMat2.Clone : v4.Text = "Ergbnis"
            v4.Show()
        End If
        Return True
    End Function

    Private Function MyWatershedBoth(resurce As Mat, result As Mat, rev As Mat, Optional debug As Boolean = False) As Boolean
        Dim ImgOfInterest As New Mat
        Dim Laplace32F As Mat
        Dim Result32F As Mat
        Dim IMG32F As Mat
        Dim Laplace16s As Mat
        Dim Result16s As Mat
        Dim Result8U As New Mat
        Dim GrayImg As New Mat
        Dim BinaerImg As New Mat
        Dim DistImg As New Mat
        Dim DistImgClear As New Mat
        Dim DistImg8u As New Mat
        Dim Markers As New Mat
        Dim Mask As New Mat
        Dim TmpResult As New Mat
        Dim dst As New Mat
        Dim Display As New Mat

        Dim testcolor_mask As Mat
        Dim testColor_Offset As Mat
        Dim testcolo_laplace32 As Mat
        Dim testcolor_img32f As Mat
        Dim testcolor_result32 As Mat
        Dim testcolor_result8 As New Mat
        Dim testcolor_markers As New Mat

        Const cThreshhold1 = 10
        Const cThreshhold2 = 0.1 '0.3



        '1. Hintergrung Ausblenden über Tiefe dann Kiste Ausmaskieren
        ' Über Tiefe und Treshold
        ImgOfInterest_Tiefe(resurce, ImgOfInterest, CInt(num_ThreshHoch.Value), CInt(num_ThreshTief.Value))
        'Maske
        ImgOfInterest = Maskieren(ImgOfInterest, CInt(num_MaskH.Value), CInt(num_MaskV.Value), CInt(num_CamOffset.Value))
        If debug Then
            ImgOfInterest.ConvertTo(Display, DepthType.Cv8U)
            Dim v2 As New ImageViewer
            v2.Image = Display.Clone : v2.Text = "ImgOfInterest"
            v2.Show()
        End If


        '2. Bild Aufbereiten (laplace)
        Dim Kernel As New Matrix(Of Single)(New Single(,) {
                                            {1, 1, 1},
                                            {1, -8, 1},
                                            {1, 1, 1}})
        Laplace32F = New Mat(ImgOfInterest.Size, DepthType.Cv32F, ImgOfInterest.NumberOfChannels)
        Result32F = New Mat(ImgOfInterest.Size, DepthType.Cv32F, ImgOfInterest.NumberOfChannels)
        CvInvoke.Filter2D(ImgOfInterest, Laplace32F, Kernel, New Point(-1, -1))
        IMG32F = New Mat : ImgOfInterest.ConvertTo(IMG32F, DepthType.Cv32F)
        CvInvoke.Subtract(IMG32F, Laplace32F, Result32F)
        'zurückwandeln
        Laplace16s = New Mat
        Result16s = New Mat
        Laplace32F.ConvertTo(Laplace16s, DepthType.Cv16S)
        Result32F.ConvertTo(Result16s, DepthType.Cv16S)
        Result32F.ConvertTo(Result8U, DepthType.Cv8U)
        If debug Then
            CvInvoke.Imshow("Result32", Result32F)
            CvInvoke.Imshow("Result16", Result16s)
            CvInvoke.Imshow("Result8", Result8U)
        End If
        CvInvoke.GaussianBlur(Result16s, Result16s, New Drawing.Size(5, 5), 3)

        'Test-------------------------------------------------------------------------
        testcolor_mask = Maskieren(_MatColor, CInt(num_MaskH.Value), CInt(num_MaskV.Value))
        testColor_Offset = ImageVerschieben(testcolor_mask, CInt(num_CamOffset.Value))

        Dim Kernel2 As New Matrix(Of Single)(New Single(,) {
                                            {1, 1, 1},
                                            {1, -8, 1},
                                            {1, 1, 1}})
        testcolo_laplace32 = New Mat(testColor_Offset.Size, DepthType.Cv32F, testColor_Offset.NumberOfChannels)
        testcolor_result32 = New Mat(testColor_Offset.Size, DepthType.Cv32F, testColor_Offset.NumberOfChannels)
        CvInvoke.Filter2D(testColor_Offset, testcolo_laplace32, Kernel2, New Point(-1, -1))
        testcolor_img32f = New Mat : testColor_Offset.ConvertTo(testcolor_img32f, DepthType.Cv32F)
        CvInvoke.Subtract(testcolor_img32f, testcolo_laplace32, testcolor_result32)
        'zurückwandeln

        testcolor_result32.ConvertTo(testcolor_result8, DepthType.Cv8U)
        CvInvoke.Imshow("Result8_Color", testcolor_result8)
        'Test-------------------------------------------------------------------------

        '3. Graustufen- & Binäresbild erzeugen
        If Result16s.NumberOfChannels = 3 Then
            CvInvoke.CvtColor(Result16s, GrayImg, ColorConversion.Bgr2Gray, 1)
        Else
            Result16s.CopyTo(GrayImg)
        End If
        GrayImg.ConvertTo(Result8U, DepthType.Cv8U)

        CvInvoke.Threshold(Result8U, BinaerImg, cThreshhold1, 255, ThresholdType.Binary Or ThresholdType.Otsu) 'benötigt cv8u
        If debug Then
            CvInvoke.Imshow("Result8", Result8U)
            CvInvoke.Imshow("BinaerImg", BinaerImg)
        End If


        '4. Distanc Funktion & normalize
        CvInvoke.DistanceTransform(BinaerImg, DistImg, Nothing, DistType.L2, 3) 'Mask size muss 0 || 3 ||5 sein  'CvInvoke.DistanceTransform(BinaerImg, DistImg, Nothing, DistType.L2, 3)
        CvInvoke.Normalize(DistImg, DistImg, 0, 1.0, NormType.MinMax)
        'Test---------------------------------------
        Dim test As New Mat
        CvInvoke.Normalize(DistImg, test, 1.0, 0, NormType.MinMax)
        If debug Then
            CvInvoke.Imshow("DistImg", DistImg)
            CvInvoke.Imshow("test", test)
        End If


        '5. Bild bereinigen (mit Treshold) und in CV8 wandeln
        CvInvoke.Threshold(DistImg, DistImgClear, cThreshhold2, 1.0, ThresholdType.Binary) '0.3

        Dim kernel1 = Mat.Ones(3, 3, DepthType.Cv8U, 1)
        CvInvoke.Dilate(DistImgClear, DistImgClear, kernel1, New Point(-1, -1), 1, BorderType.Default, New MCvScalar(0))
        If debug Then
            CvInvoke.Imshow("DistImgClear", DistImgClear)
        End If

        CvInvoke.Normalize(DistImgClear, DistImg8u, 0, 255, NormType.MinMax, DepthType.Cv8U)

        '6. Objekte bezeichnen
        Dim Contours As New VectorOfVectorOfPoint
        CvInvoke.FindContours(DistImg8u, Contours, Nothing, RetrType.List, ChainApproxMethod.ChainApproxSimple)
        lb_Info.Items.Insert(0, $"Es wurden {Contours.Size} Kontuen Gefunden")
        ' Create the marker image for the watershed algorithm
        Markers = New Mat(resurce.Size, DepthType.Cv32S, 1) ' Zur Anzeige farbiger Bilder
        'Test-------------------------------------------------------------------------
        'Markers.SetTo(New MCvScalar(0)) ' AnzeigenBild auf schwarz setzen
        Markers.SetTo(New MCvScalar(-1)) ' AnzeigenBild auf schwarz setzen
        'Test-------------------------------------------------------------------------
        For i = 0 To Contours.Size - 1
            Dim Colo = New MCvScalar((i + 1) * 10) ' An 1 nummerieren. 0 ist ja der Hintergrund
            CvInvoke.DrawContours(Markers, Contours, i, Colo, -1) ' -1 damit die contour ausgefüllt wird
        Next
        If debug Then
            Dim Maskshow As New Mat
            Markers.ConvertTo(Maskshow, DepthType.Cv8U)
            CvInvoke.Imshow("markers32S1", Maskshow)
        End If

        CvInvoke.Circle(Markers, New Point(5, 5), 3, New MCvScalar(255), -1)
        If debug Then
            Dim Maskshow2 As New Mat
            Markers.ConvertTo(Maskshow2, DepthType.Cv8U)
            CvInvoke.Imshow("markers32S1_2", Maskshow2)
        End If
        testcolor_markers = Markers.Clone

        '7. Watershed zu objekt Auswertung  Unklar ob Result8U oder DistImg8u
        Dim imgWater8U3 As New Mat
        If Result8U.NumberOfChannels = 3 Then

            imgWater8U3 = Result8U
        Else
            CvInvoke.CvtColor(Result8U, imgWater8U3, ColorConversion.Gray2Bgr, 3)
        End If

        ' CvInvoke.WaitKey(0)
        CvInvoke.Watershed(imgWater8U3, Markers)
        Dim Maskshow3 As New Mat
        Markers.ConvertTo(Maskshow3, DepthType.Cv8U)
        CvInvoke.Imshow("Watershed", Maskshow3)
        'Test-------------------------------------------------------------------------
        imgWater8U3 = testcolor_result8
        CvInvoke.Watershed(imgWater8U3, testcolor_markers)
        Dim Maskshow4 As New Mat
        testcolor_markers.ConvertTo(Maskshow4, DepthType.Cv8U)
        CvInvoke.Imshow("Watershed_Color", Maskshow4)
        'Test-------------------------------------------------------------------------

        '8. Farbenvergeben
        Dim Nc = Contours.Size ' Anzahl Contours = Anzahl an markierten Elementen
        Dim Cols(Nc) As MCvScalar : Dim RND As New Random 'Dim Cols(Nc - 1) As MCvScalar : Dim RND As New Random
        For i = 0 To Nc '- 1
            Cols(i) = New Bgr(RND.Next(0, 256), RND.Next(0, 256), RND.Next(0, 256)).MCvScalar
        Next

        Dim ZeichenMat2 = Mat.Zeros(Markers.Rows, Markers.Cols, DepthType.Cv8U, 3)
        For Zeile = 0 To Markers.Rows - 1
            For Spalte = 0 To Markers.Cols - 1
                Dim wert As Int32

                wert = UmwandlungClass.GetInt32Value(Markers, Zeile, Spalte)

                Dim Marke As Int32 = UmwandlungClass.GetInt32Value(Markers, Zeile, Spalte)
                Dim gefunden As Boolean = False
                If Marke > 0 And Marke <= Nc * 10 Then
                    Dim ByteWerte() As Byte = {CByte(Cols(Marke \ 10).V0), CByte(Cols(Marke \ 10).V1), CByte(Cols(Marke \ 10).V2)}
                    'Objekt eintragen
                    If _MyObjekte.Count < 1 Then
                        Dim tmpObjekt As New MyObekt(wert, ByteWerte)
                        Dim tiefe As Int32 = UmwandlungClass.GetInt16Value(_MatDepth, Zeile, Spalte)
                        tmpObjekt.Add_Ref(Spalte, Zeile, tiefe)
                        _MyObjekte.Add(tmpObjekt)
                    Else
                        For Each ob As MyObekt In _MyObjekte
                            If wert = ob.ID Then
                                gefunden = True
                                Dim tiefe As Int32 = UmwandlungClass.GetInt16Value(_MatDepth, Zeile, Spalte)
                                ob.Add_Ref(Spalte, Zeile, tiefe)
                            End If
                        Next
                        If Not gefunden Then
                            Dim tmpObjekt As New MyObekt(wert, ByteWerte)
                            Dim tiefe As Int32 = UmwandlungClass.GetInt16Value(_MatDepth, Zeile, Spalte)
                            tmpObjekt.Add_Ref(Spalte, Zeile, tiefe)
                            _MyObjekte.Add(tmpObjekt)
                        End If
                    End If

                    UmwandlungClass.SetByteValues(ZeichenMat2, Zeile, Spalte, ByteWerte)
                End If
            Next
        Next
        'Test-------------------------------------------------------------------------
        Dim ZeichenMat3 = Mat.Zeros(testcolor_markers.Rows, testcolor_markers.Cols, DepthType.Cv8U, 3)
        For Zeile = 0 To testcolor_markers.Rows - 1
            For Spalte = 0 To testcolor_markers.Cols - 1

                Dim Marke As Int32 = UmwandlungClass.GetInt32Value(testcolor_markers, Zeile, Spalte)
                If Marke > 0 And Marke <= Nc * 10 Then
                    Dim ByteWerte() As Byte = {CByte(Cols(Marke \ 10).V0), CByte(Cols(Marke \ 10).V1), CByte(Cols(Marke \ 10).V2)}
                    UmwandlungClass.SetByteValues(ZeichenMat3, Zeile, Spalte, ByteWerte)
                End If
            Next
        Next
        CvInvoke.Resize(ZeichenMat3, ZeichenMat3, New Size(640, 480))
        ib_res_02.Image = ZeichenMat3.Clone
        'Test-------------------------------------------------------------------------
        For Each ob2 As MyObekt In _MyObjekte
            LB_obj.ForeColor = Color.FromArgb(0, ob2.Color(2), ob2.Color(1), ob2.Color(0)) 'ob2.color ist bgr Farbe form arg erwartet argb Farbe
            LB_obj.Items.Add(ob2.ToString)
        Next
        ZeichenMat2.CopyTo(result)
        If debug Then
            CvInvoke.Imshow("Ergebnis", ZeichenMat2)
        End If

        '9. Draw Points
        If cb_DrawPoint.Checked Then
            For Each obj As MyObekt In _MyObjekte
                'X min => Grün
                CvInvoke.Circle(ZeichenMat2, New Point(obj.Min_X.X, obj.Min_X.Y), 6, New MCvScalar(255, 255, 255), 3)
                CvInvoke.Circle(ZeichenMat2, New Point(obj.Min_X.X, obj.Min_X.Y), 1, New MCvScalar(0, 255, 0), 1) 'Gün
                'X max => GrünRot
                CvInvoke.Circle(ZeichenMat2, New Point(obj.Max_X.X, obj.Max_X.Y), 6, New MCvScalar(255, 255, 255), 3)
                CvInvoke.Circle(ZeichenMat2, New Point(obj.Max_X.X, obj.Max_X.Y), 1, New MCvScalar(0, 255, 255), 1) 'Gün
                'Y min => Blau
                CvInvoke.Circle(ZeichenMat2, New Point(obj.Min_Y.X, obj.Min_Y.Y), 6, New MCvScalar(255, 255, 255), 3)
                CvInvoke.Circle(ZeichenMat2, New Point(obj.Min_Y.X, obj.Min_Y.Y), 1, New MCvScalar(255, 0, 0), 1) 'Blau
                'Y max => BlauRot
                CvInvoke.Circle(ZeichenMat2, New Point(obj.Max_Y.X, obj.Max_Y.Y), 6, New MCvScalar(255, 255, 255), 3)
                CvInvoke.Circle(ZeichenMat2, New Point(obj.Max_Y.X, obj.Max_Y.Y), 1, New MCvScalar(255, 0, 255), 1) 'Blau
                'Virtueller Start => Rot
                CvInvoke.Circle(ZeichenMat2, New Point(obj.Min_X.X, obj.Min_Y.Y), 6, New MCvScalar(255, 255, 255), 3)
                CvInvoke.Circle(ZeichenMat2, New Point(obj.Min_X.X, obj.Min_Y.Y), 1, New MCvScalar(0, 0, 255), 1) 'Rot
                'Virtuelles Ende => Rotgrünblau
                CvInvoke.Circle(ZeichenMat2, New Point(obj.Max_X.X, obj.Max_Y.Y), 6, New MCvScalar(255, 255, 255), 3)
                CvInvoke.Circle(ZeichenMat2, New Point(obj.Max_X.X, obj.Max_Y.Y), 1, New MCvScalar(125, 125, 255), 1) 'Rot
            Next
            Dim v4 As New ImageViewer
            v4.Image = ZeichenMat2.Clone : v4.Text = "Ergbnis"
            v4.Show()
        End If
        Return True
    End Function

    Private Function ImageOfInterest(resurce As Mat, result As Mat, Optional rev As Mat = Nothing, Optional dif As Boolean = False, Optional mask As Boolean = False, Optional depth As Boolean = False, Optional gaus As Boolean = False, Optional offset As Int32 = 0, Optional number As Int32 = 1) As Boolean
        Dim Ergebnis As Boolean = True
        Dim tmp_Mat As New Mat
        tmp_Mat = resurce
        'Differenz
        If dif And Ergebnis Then
            If rev IsNot Nothing Then
                CvInvoke.AbsDiff(tmp_Mat, rev, tmp_Mat)
            Else
                lb_Info.Items.Insert(0, "IOIFehler keine Reverenz übergeben")
                Ergebnis = False
            End If
        End If
        'Maskierung
        If mask And Ergebnis Then
            tmp_Mat = Maskieren(tmp_Mat, CInt(num_MaskH.Value), CInt(num_MaskV.Value), offset)
        End If
        'Tiefenfilder
        If depth And Ergebnis Then
            CvInvoke.Threshold(tmp_Mat, tmp_Mat, CInt(num_ThreshHoch.Value), 100, ThresholdType.ToZero)
            CvInvoke.Threshold(tmp_Mat, tmp_Mat, CInt(num_ThreshTief.Value), 100, ThresholdType.ToZeroInv)
        End If
        'Gaus
        If gaus And Ergebnis Then
            CvInvoke.GaussianBlur(tmp_Mat, tmp_Mat, New Drawing.Size(3, 3), 2)
        End If
        If number = 2 Then
            ib_ImOfIn_02.Image = tmp_Mat.Clone
        Else
            ib_ImOfIn_01.Image = tmp_Mat.Clone
        End If
        result = tmp_Mat
        Return Ergebnis
    End Function

End Class 'Form1