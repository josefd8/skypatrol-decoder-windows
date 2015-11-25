Imports System
Imports System.Threading
Imports System.Net.Sockets
Imports System.Net
Imports System.IO
Imports System.Text
Imports Libreria.ClassGeneral


''' <summary>
''' Esta clase implementa los metodos neesarios para generar un socket de escucha TCP, asi como metodos asinronos para el
''' manejo de los datos
''' </summary>
''' <remarks></remarks>
Public Class ClassTCPServer

#Region "Estructuras"
    ''' <summary>
    ''' Esta estructura permite guardar la información sobre un cliente
    ''' </summary>
    ''' <remarks></remarks>
    Public Structure InfoDeUnCliente
        ''' <summary>
        ''' Socket utilizado para mantener la conexion con el cliente 
        ''' </summary>
        ''' <remarks></remarks>
        Public Socket As Socket
        ''' <summary>
        ''' Thread utilizado para escuchar al cliente 
        ''' </summary>
        ''' <remarks></remarks>
        Public Thread As Thread
        ''' <summary>
        ''' Ultimos datos enviados por el cliente
        ''' </summary>
        ''' <remarks></remarks>
        Public UltimosDatosRecibidos As String
        ''' <summary>
        ''' Ultimos datos enviados por el cliente 
        ''' </summary>
        ''' <remarks></remarks>
        Public DatosRecibidosBytes() As Byte
        ''' <summary>
        ''' Hora en que se establecio la conexión
        ''' </summary>
        ''' <remarks></remarks>
        Public TimeInicio As Date
        ''' <summary>
        ''' Ip del cliente remoto como un objeto IPEndPoint
        ''' </summary>
        ''' <remarks></remarks>
        Public IpTerminal As Net.IPEndPoint
        Public running As Boolean
    End Structure
#End Region

#Region "Variables"
    Private tcpLsn As TcpListener
    Private Clientes As New Hashtable   'Aqui se guarda la informacion de todos los clientes conectados 
    Private tcpThd As Thread
    Private IDClienteActual As Net.IPEndPoint 'Ultimo cliente conectado 
    Private m_PuertoDeEscucha As String
    Private m_IpDeEscucha As IPAddress
    Private m_TablaConexiones As New Hashtable
    Private m_Clientes As Hashtable
    Public Estado As Boolean = False
    Private mensaje_error As String
    Private run As Boolean = True
    Private esp_cli As Boolean
#End Region

#Region "Eventos"
    ''' <summary>
    ''' Este metodo se dispara de forma asinrona cada vez que un cliente remoto inicia una conexion con el servidor
    ''' </summary>
    ''' <param name="IDTerminal">IP y puerto del cliente remoto como un objeto IPEndpoint</param>
    ''' <remarks></remarks>
    Public Event NuevaConexion(ByVal IDTerminal As Net.IPEndPoint)
    ''' <summary>
    ''' Este metodo se dispara de forma asinrona cada vez que un cliente envia datos al servidor
    ''' </summary>
    ''' <param name="IDTerminal">IP y puerto del cliente remoto como un objeto IPEndpoint</param>
    ''' <remarks></remarks>
    Public Event DatosRecibidos(ByVal IDTerminal As Net.IPEndPoint)
    ''' <summary>
    ''' Este metodo se dispara de forma asinrona cada vez que un cliente envia datos al servidor
    ''' </summary>
    ''' <param name="datos">Parametros del mensaje entrante e identificacion del cliente como un objeto tipo Paquete_entrante</param>
    ''' <remarks></remarks>
    Public Event Datos_Recibidos(ByVal datos As Paquete_Entrante)
    ''' <summary>
    ''' Este metodo se dispara cuando un cliente finaliza la conexion con el servidor
    ''' </summary>
    ''' <param name="IDTerminal">IP y puerto del cliente remoto como un objeto IPEndpoint</param>
    ''' <remarks></remarks>
    Public Event ConexionTerminada(ByVal IDTerminal As Net.IPEndPoint)
    ''' <summary>
    ''' Este metodo se dispara si se enuentra un error durante la conexion de un cliente o la escucha de datos
    ''' </summary>
    ''' <param name="Mensaje">Mensaje generado</param>
    ''' <remarks></remarks>
    Public Event Error_Generado(ByVal Mensaje As String)
#End Region

#Region "Propiedades"

    Public ReadOnly Property ERROR_Mensaje As String
        Get
            Return Me.mensaje_error
        End Get
    End Property

    'Asignar Puerto
    Property PuertoDeEscucha() As String
        Get
            PuertoDeEscucha = m_PuertoDeEscucha
        End Get
        Set(ByVal Value As String)
            m_PuertoDeEscucha = Value
        End Set
    End Property

    'Asignar Ip
    Property IpDeEscucha() As IPAddress
        Get
            IpDeEscucha = m_IpDeEscucha
        End Get
        'Set(ByVal Value As String)
        Set(ByVal Value As IPAddress)
            m_IpDeEscucha = Value
        End Set
    End Property

    'Verificar Conexiones
    Property TablaConexiones() As Hashtable
        Get
            SyncLock Me.m_TablaConexiones
                Return Hashtable.Synchronized(Me.m_TablaConexiones)
            End SyncLock
        End Get
        Set(ByVal Value As Hashtable)
            Me.m_TablaConexiones = Value
        End Set
    End Property

    'Verificar Conexiones
    ReadOnly Property ConexionesActivas() As Hashtable
        Get
            SyncLock Me.Clientes
                Return Hashtable.Synchronized(Me.Clientes)
            End SyncLock
        End Get
    End Property

#End Region

    Public Enum Accion
        Desconectar
        Escuchar
    End Enum

#Region "Métodos"

    ''' <summary>
    ''' Inicia el proceso de escuha por el socket
    ''' </summary>
    ''' <param name="Accion"></param>
    ''' <remarks></remarks>
    Public Sub Escuchar(ByVal Accion As Accion)
        If Accion = ClassTCPServer.Accion.Escuchar Then
            Try
                Me.tcpLsn = New TcpListener(IpDeEscucha, PuertoDeEscucha)
                'Inicio la escucha 
                tcpLsn.Start()
                'Creo un thread para que se quede escuchando la llegada de un cliente 
                Me.tcpThd = New Thread(AddressOf Me.EsperarCliente)
                Me.esp_cli = True
                Me.tcpThd.Start()
                Me.Estado = True
            Catch ex As Exception
                Me.Estado = False
                RaiseEvent Error_Generado(ex.Message)
                Me.mensaje_error = "Escuchar " & ex.Message
            End Try
        Else
            Try
                Me.tcpThd.Abort()
            Catch ex As Exception

            End Try
            Try
                Me.tcpLsn.Stop()
            Catch ex As Exception

            End Try
            Me.Cerrar()
        End If
    End Sub

    ''' <summary>
    ''' Retorna el ultimo mensaje recibido por un cliente en formado String
    ''' </summary>
    ''' <param name="IDCliente">IP y puerto del cliente como un objeto IPEndPoint</param>
    ''' <returns>Ultimo mensaje como un String</returns>
    ''' <remarks></remarks>
    Public Function ObtenerDatos_S(ByVal IDCliente As Net.IPEndPoint) As String
        Dim InfoClienteSolicitado As InfoDeUnCliente
        'Obtengo la informacion del cliente solicitado 
        InfoClienteSolicitado = Clientes(IDCliente)
        Return InfoClienteSolicitado.UltimosDatosRecibidos
        'InfoClienteSolicitado.
    End Function

    ''' <summary>
    ''' Retorna el ultimo mensaje recibido por un cliente como un arreglo de bytes
    ''' </summary>
    ''' <param name="IDCliente">IP y puerto del cliente como un objeto IPEndPoint</param>
    ''' <param name="Datos">Ultimo mensaje como un arreglo de bytes</param>
    ''' <remarks></remarks>
    Public Sub ObtenerDatos_B(ByVal IDCliente As Net.IPEndPoint, ByRef Datos() As Byte)
        Dim InfoClienteSolicitado As InfoDeUnCliente
        'Obtengo la informacion del cliente solicitado 
        InfoClienteSolicitado = Clientes(IDCliente)
        Datos = InfoClienteSolicitado.DatosRecibidosBytes
        'InfoClienteSolicitado.
    End Sub

    ''' <summary>
    ''' Fuerza la desconexion del cliente identificado por el objeto IPEndPoint
    ''' </summary>
    ''' <param name="IDCliente">IP y puerto del cliente remoto a desconectar como un objeto IPEndPoint</param>
    ''' <param name="all">Si true, se elimina toda la informacion de ese cliente de memoria</param>
    ''' <remarks></remarks>
    Public Sub Cerrar(ByVal IDCliente As Net.IPEndPoint, Optional ByVal all As Boolean = False)
        Dim InfoClienteActual As InfoDeUnCliente
        'Obtengo la informacion del cliente solicitado 
        Try
            InfoClienteActual = Me.Clientes(IDCliente)
            'Cierro la conexion con el cliente 
            InfoClienteActual.Socket.Close()
            'InfoClienteActual.Thread.Sleep(10)
            Thread.Sleep(10)
            InfoClienteActual.running = False
            'InfoClienteActual.Thread.Abort()
            'CerrarThread(IDCliente)
            SyncLock Me
                'Elimino el cliente del HashArray que guarda la informacion de los clientes 
                If Not all Then
                    Me.Clientes.Remove(IDCliente)
                End If
            End SyncLock
            'Clientes.Remove(IDCliente)
        Catch ex As Exception
            'Error cerrando conexion
        End Try
    End Sub

    ''' <summary>
    ''' Cierra el socket TP y desconecta a todos los clientes conectador
    ''' </summary>
    ''' <remarks></remarks>
    Public Sub Cerrar()
        Try
            If Not Me.tcpThd Is Nothing Then
                Me.run = False
                Me.esp_cli = False
                Me.tcpLsn.Stop()
                Me.tcpLsn = Nothing
                Me.tcpThd = Nothing
                'Recorro todos los clientes y voy cerrando las conexiones 
                For Each InfoClienteActual As InfoDeUnCliente In Me.Clientes.Values
                    Me.Cerrar(InfoClienteActual.Socket.RemoteEndPoint, True)
                Next
                Me.Clientes.Clear()
            End If
        Catch ex As Exception
            Me.mensaje_error = "Cerrar " & ex.Message
        End Try
    End Sub

    ''' <summary>
    ''' Envia datos al cliente identificado por el objeto IPEndPoint
    ''' </summary>
    ''' <param name="IDCliente">IP y puerto del cliente como un objeto IPEndPoint</param>
    ''' <param name="Datos">Datos a enviar en formato String</param>
    ''' <remarks></remarks>
    Public Sub EnviarDatos(ByVal IDCliente As Net.IPEndPoint, ByVal Datos As String)
        Try
            Dim Cliente As InfoDeUnCliente
            'Obtengo la informacion del cliente al que se le quiere enviar el mensaje 
            Cliente = Clientes(IDCliente)
            'Le envio el mensaje 
           
            If Not Cliente.Socket Is Nothing Then
                Cliente.Socket.Send(Encoding.UTF8.GetBytes(Datos))
            Else
                RaiseEvent Error_Generado("No está conectado")
            End If

        Catch ex As Exception
            Clientes.Remove(IDCliente)
            Me.mensaje_error = "EnviarDatos (IP String)" & ex.Message
        End Try
    End Sub

    ''' <summary>
    ''' Envia el mismo mensaje a todos los clientes conetados el socket en ese momento (broadcast)
    ''' </summary>
    ''' <param name="Datos">Mensaje a enviar en formato String</param>
    ''' <remarks></remarks>
    Public Sub EnviarDatos(ByVal Datos As String)
        Dim Cliente As InfoDeUnCliente
        'Recorro todos los clientes conectados, y les envio el mensaje recibido 
        'en el parametro Datos 
        Try
            For Each Cliente In Clientes.Values
                EnviarDatos(Cliente.Socket.RemoteEndPoint, Datos)
            Next
        Catch ex As Exception
            Me.mensaje_error = "EnviarDatos (String) " & ex.Message
        End Try
    End Sub

    ''' <summary>
    ''' Envia el mismo mensaje a todos los clientes conetados el socket en ese momento (broadcast)
    ''' </summary>
    ''' <param name="Datos">Mensaje a enviar como una cadena de bytes</param>
    ''' <remarks></remarks>
    Public Sub EnviarDatos(ByVal Datos() As Byte)
        Dim Cliente As InfoDeUnCliente
        'Recorro todos los clientes conectados, y les envio el mensaje recibido 
        'en el parametro Datos 
        For Each Cliente In Clientes.Values
            EnviarDatos(Cliente.Socket.RemoteEndPoint, Datos)
        Next
    End Sub

    ''' <summary>
    ''' Envia un mensaje al cliente identificado por el objeto IPEndPoint
    ''' </summary>
    ''' <param name="IDCliente">cliente remoto al que se le enviara un mensaje</param>
    ''' <param name="Datos">datos a enviar como una cadena de bytes</param>
    ''' <remarks></remarks>
    Public Sub EnviarDatos(ByVal IDCliente As Net.IPEndPoint, ByVal Datos() As Byte)
        Dim Cliente As InfoDeUnCliente
        'Obtengo la informacion del cliente al que se le quiere enviar el mensaje 
        Cliente = Clientes(IDCliente)
        'Le envio el mensaje 
        If Not Cliente.Socket Is Nothing Then
            Cliente.Socket.Send(Datos)
        End If
    End Sub

    Public Function ConectarEnviarCliente(ByVal server As [String], ByVal Port As [Int32], ByVal message As String) As Boolean
        Dim Datos() As Byte = {}
        Dim Temp() As String
        Dim J As Integer

        Temp = message.Split(",")
        For J = 0 To UBound(Temp)
            ReDim Preserve Datos(J)
            Datos(J) = Val("&H" & Temp(J))
        Next
        Try

            Dim client As New TcpClient(server, Port)
            Dim data As [Byte]() = Datos
            Dim stream As NetworkStream = client.GetStream()

            stream.Write(data, 0, data.Length)

            Console.WriteLine("Sent: {0}", message)

            Return True
            client.Close()
        Catch e As ArgumentNullException
            Console.WriteLine("ArgumentNullException: {0}", e)
            Me.mensaje_error = "ConectarEnviarCliente " & e.Message
            Return False
        Catch e As SocketException
            Console.WriteLine("SocketException: {0}", e)
            Me.mensaje_error = "ConectarEnviarCliente " & e.Message
            Return False
        End Try

    End Function

#End Region

#Region "Funciones Privadas"

    Private Sub EsperarCliente()
        Dim InfoClienteActual As New InfoDeUnCliente
       
        With InfoClienteActual
            While Me.esp_cli
                'Establesco el timeout para recibir datos
                '.Socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Linger, lingerOption)
                'Cuando se recibe la conexion, guardo la informacion del cliente 
                'Guardo el Socket que utilizo para mantener la conexion con el cliente 
                'tcpLsn.
                Try
                    .Socket = tcpLsn.AcceptSocket() 'Se queda esperando la conexion de un cliente 
                    'Guardo el el RemoteEndPoint, que utilizo para identificar al cliente 
                    IDClienteActual = .Socket.RemoteEndPoint
                    'Creo un Thread para que se encargue de escuchar los mensaje del cliente 
                    .Thread = New Thread(AddressOf LeerSocket)
                    'Agrego la informacion del cliente al HashArray Clientes, donde esta la 
                    'informacion de todos estos 

                    .TimeInicio = Now()
                    .IpTerminal = IDClienteActual
                    SyncLock Me
                        Clientes.Add(IDClienteActual, InfoClienteActual)
                    End SyncLock
                    TablaConexiones = Clientes
                    'Genero el evento Nueva conexion 
                    RaiseEvent NuevaConexion(IDClienteActual)
                    'Inicio el thread encargado de escuchar los mensajes del cliente 
                    .Thread.Start()
                    Thread.Sleep(500)
                Catch ex As Exception
                    'Me.Estado = Me.Estado
                End Try
            End While
        End With
    End Sub

    Private Sub LeerSocket()
        Dim IDReal As Net.IPEndPoint 'ID del cliente que se va a escuchar 
        Dim Recibir() As Byte 'Array utilizado para recibir los datos que llegan 
        Dim InfoClienteActual As InfoDeUnCliente 'Informacion del cliente que se va escuchar 
        Dim Ret As Integer = 0

        IDReal = IDClienteActual
        InfoClienteActual = Clientes(IDReal)
        InfoClienteActual.running = True
        With InfoClienteActual
            'While True
            While InfoClienteActual.running
                If .Socket Is Nothing Then
                    Exit Sub
                End If
                If .Socket.Connected Then
                    Recibir = New Byte(16384) {}
                    Try
                        'Me quedo esperando a que llegue un mensaje desde el cliente 
                        Ret = .Socket.Receive(Recibir, Recibir.Length, SocketFlags.None)
                        If Ret > 0 Then

                            Array.Resize(Recibir, Ret)

                            .UltimosDatosRecibidos = Encoding.ASCII.GetString(Recibir)
                            .DatosRecibidosBytes = Recibir
                            .TimeInicio = Now

                            Clientes(IDReal) = InfoClienteActual
                            'Genero el evento de la recepcion del mensaje 

                            Dim datos_entrantes As New Paquete_Entrante
                            datos_entrantes.Ip = IDReal.Address.ToString
                            datos_entrantes.Puerto = IDReal.Port
                            ObtenerDatos_B(IDReal, datos_entrantes.Datos_Byte)
                            datos_entrantes.cliente = IDReal
                            datos_entrantes.Datos_Str = .UltimosDatosRecibidos
                            datos_entrantes.Datos_Byte = .DatosRecibidosBytes

                            RaiseEvent Datos_Recibidos(datos_entrantes)

                            RaiseEvent DatosRecibidos(IDReal)

                        Else
                            'Genero el evento de la finalizacion de la conexion 
                            RaiseEvent ConexionTerminada(IDReal)
                            Me.Cerrar(IDReal)
                            Exit While
                        End If
                    Catch e As Exception

                        Me.mensaje_error = "LeerSocket " & e.Message
                        If Not .Socket.Connected Then
                            'Genero el evento de la finalizacion de la conexion 
                            RaiseEvent ConexionTerminada(IDReal)
                            Exit While
                        End If
                    End Try
                End If
            End While
            Call CerrarThread(IDReal)
        End With
    End Sub

    Private Sub CerrarThread(ByVal IDCliente As Net.IPEndPoint)
        Dim InfoClienteActual As InfoDeUnCliente
        'Cierro el thread que se encargaba de escuchar al cliente especificado 
        InfoClienteActual = Clientes(IDCliente)
        Try
            InfoClienteActual.Thread.Abort()
            'InfoClienteActual.Thread = Nothing
        Catch e As Exception
            Me.mensaje_error = "CerrarThread " & e.Message
            SyncLock Me
                'Elimino el cliente del HashArray que guarda la informacion de los clientes 
                Clientes.Remove(IDCliente)
            End SyncLock
        End Try
    End Sub
#End Region

End Class

