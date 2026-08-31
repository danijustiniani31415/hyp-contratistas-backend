-- Ingreso directo FFT: la vacante ya no pasa por ninguna aprobación, la registre quien la
-- registre. Antes solo se saltaba «Aprobaciones» cuando el pedido lo hacía el propio Gerente
-- General (no se aprueba a sí mismo); ahora se lo salta siempre, porque en un FFT no hay nada que
-- decidir — quien pide ya nombró a la persona por su nombre y su documento.
--
-- Con eso el correo FFT_SOLICITUD_GG dejó de ser "el pedido del Gerente General" y pasó a ser el
-- aviso a GTH de CUALQUIER ingreso directo, así que su nombre y su descripción (los que se leen en
-- Configuración de Solicitud de Personal) quedaron mintiendo. Es lo ÚNICO que hay que correr en la
-- base: el cambio es de flujo y no de esquema. No hay tablas, columnas ni índices nuevos.
--
-- Ojo con los destinatarios: hasta ahora este correo solo salía en los pedidos del Gerente General,
-- así que puede no tener ninguno configurado. Desde este cambio sale en todo ingreso directo — si
-- la lista está vacía el requerimiento igual se registra y aparece en la bandeja de Reclutamiento,
-- pero GTH no recibe el aviso. El SELECT del final lo deja ver.
--
-- Aplicado en dev el 2026-08-27.

UPDATE gth_correo_tipo
   SET nombre            = 'Candidato de ingreso directo FFT',
       descripcion       = 'Avisa a GTH el candidato de una vacante de ingreso directo FFT. No pasa por aprobación: sale al registrarse la solicitud, lo pida quien lo pida.',
       updated_date_time = now()
 WHERE codigo = 'FFT_SOLICITUD_GG'
   AND state;

-- Verificación: el texto nuevo y a quién le llega hoy ese correo.
SELECT t.codigo,
       t.nombre,
       t.descripcion,
       d.email,
       d.es_copia,
       d.active
  FROM gth_correo_tipo t
  LEFT JOIN gth_correo_destinatario d
         ON d.gth_correo_tipo_id = t.gth_correo_tipo_id
        AND d.state
 WHERE t.codigo = 'FFT_SOLICITUD_GG'
   AND t.state
 ORDER BY d.es_copia, d.email;
