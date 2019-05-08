Module Referenzierung
    Public Function RefCalcFactor(val1Cam As Single, val1Robot As Single, val2Cam As Single, val2Robot As Single, valZCam As Int32) As Double
        Return (val1Robot - val2Robot) / (val1Cam - val2Cam) * valZCam
    End Function

    Public Function RefCalcOffset(val1Cam As Single, val1Robot As Single, val2Cam As Single, val2Robot As Single) As Double
        Return (val2Robot * val1Cam - val1Robot * val2Cam) / (val1Cam - val2Cam)
    End Function

    Public Function RefCalcXY(ByRef refList As List(Of MyRefObjekt), ByRef factorX As Double, ByRef offsetX As Double, ByRef factorY As Double, ByRef offsetY As Double) As Boolean
        If refList.Count <= 1 Then Return False
        Dim tmpFactorX, tmpFactorY, tmpOffsetX, tmpOffsetY As Double
        Dim cnt As Int32 = 0
        For i = 0 To refList.Count - 2
            For k = i + 1 To refList.Count - 1
                Dim entry1 As MyRefObjekt = refList(i)
                Dim entry2 As MyRefObjekt = refList(k)
                tmpFactorX += RefCalcFactor(entry1.PunktCam.X, entry1.PunktRobo.Y, entry2.PunktCam.X, entry2.PunktRobo.Y, CInt((entry1.ZinUnits + entry2.ZinUnits) / 2))
                tmpFactorY += RefCalcFactor(entry1.PunktCam.Y, entry1.PunktRobo.X, entry2.PunktCam.Y, entry2.PunktRobo.X, CInt((entry1.ZinUnits + entry2.ZinUnits) / 2))
                tmpOffsetX += RefCalcOffset(entry1.PunktCam.X, entry1.PunktRobo.Y, entry2.PunktCam.X, entry2.PunktRobo.Y)
                tmpOffsetY += RefCalcOffset(entry1.PunktCam.Y, entry1.PunktRobo.X, entry2.PunktCam.Y, entry2.PunktRobo.X)
                cnt += 1
            Next
        Next
        'Ausgeben
        factorX = tmpFactorX / cnt
        factorY = tmpFactorY / cnt
        offsetX = tmpOffsetX / cnt
        offsetY = tmpOffsetY / cnt

        Return True
    End Function

    Public Function RefCalcZ(detectedObjects As List(Of MyObjektV2), referenceObjects As List(Of MyRefObjekt), ByRef factorZ As Double) As Boolean
        If detectedObjects.Count <> referenceObjects.Count Then Return False
        Dim tmpFactor As Double

        Dim list1, list2 As New List(Of Integer)
        For Each obj In detectedObjects
            list1.Add(obj.GetDepthVal)
        Next
        For Each obj In referenceObjects
            list2.Add(obj.ZinMM)
        Next

        list1.Sort()
        list2.Sort()
        list1.Reverse()

        ' Calc Factor
        For i = 0 To list1.Count - 1
            tmpFactor += list2(i) / list1(i)
        Next
        factorZ = tmpFactor / list1.Count

        Return True
    End Function
End Module
