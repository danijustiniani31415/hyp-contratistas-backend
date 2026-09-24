-- Separa "tener el permiso" de "que te notifiquen por correo" en lb_usuario_asignacion.
-- Sin esto, GetEmailsConPermiso (PedidoService) notifica a TODA asignación vigente con el
-- permiso, incluyendo roles asignados solo para acceso/mantenimiento (ej. sistemas con rol de
-- Gerente General para soporte, sin ser el gerente real).
ALTER TABLE lb_usuario_asignacion
  ADD COLUMN IF NOT EXISTS notificar boolean NOT NULL DEFAULT true;
