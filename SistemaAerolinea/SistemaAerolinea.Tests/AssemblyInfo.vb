Imports Xunit

' Varias pruebas cambian el usuario de la sesión (Sesion.UsuarioActual), que es
' estático y compartido por todo el proceso. Si xUnit corriera las clases en
' paralelo, una prueba podría cambiar el rol mientras otra lo está comprobando.
<Assembly: CollectionBehavior(DisableTestParallelization:=True)>
