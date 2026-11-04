import { useState } from 'react';
import { Link, Outlet } from 'react-router';
import { AppBar, Box, Chip, Drawer, IconButton, Toolbar, Typography, useMediaQuery, useTheme } from '@mui/material';
import MenuIcon from '@mui/icons-material/Menu';
import { SideMenu } from './SideMenu';
import { NotificationBell } from './NotificationBell';
import { UserMenu } from './UserMenu';
import { useAuth } from '../auth/AuthContext';

const DRAWER_WIDTH = 260;

/** 共有レイアウト：ヘッダー＋サイドメニュー（PC は常時表示、スマホ・タブレットはドロワー） */
export function AppLayout() {
  const theme = useTheme();
  const isDesktop = useMediaQuery(theme.breakpoints.up('lg'));
  const [mobileOpen, setMobileOpen] = useState(false);
  const [desktopOpen, setDesktopOpen] = useState(true);
  const auth = useAuth();
  const drawerOpen = isDesktop ? desktopOpen : mobileOpen;

  return (
    <Box sx={{ display: 'flex', minHeight: '100vh' }}>
      <AppBar position="fixed" sx={{ zIndex: (t) => t.zIndex.drawer + 1 }}>
        <Toolbar sx={{ gap: 1 }}>
          <IconButton
            color="inherit"
            edge="start"
            aria-label="メニュー"
            onClick={() => (isDesktop ? setDesktopOpen(!desktopOpen) : setMobileOpen(!mobileOpen))}
          >
            <MenuIcon />
          </IconButton>
          <Typography
            variant="h6"
            component={Link}
            to="/"
            noWrap
            sx={{ color: 'inherit', textDecoration: 'none', fontWeight: 700, letterSpacing: 1 }}
          >
            IL共有マスター
          </Typography>
          {auth.config.mode === 'Development' && (
            <Chip label="開発環境" size="small" sx={{ bgcolor: 'rgba(255,255,255,0.2)', color: 'inherit', display: { xs: 'none', sm: 'flex' } }} />
          )}
          <Box sx={{ flex: 1 }} />
          <NotificationBell />
          <UserMenu />
        </Toolbar>
      </AppBar>

      <Drawer
        variant={isDesktop ? 'persistent' : 'temporary'}
        open={drawerOpen}
        onClose={() => setMobileOpen(false)}
        ModalProps={{ keepMounted: true }}
        sx={{
          width: isDesktop && desktopOpen ? DRAWER_WIDTH : 0,
          flexShrink: 0,
          transition: theme.transitions.create('width'),
          '& .MuiDrawer-paper': { width: DRAWER_WIDTH, boxSizing: 'border-box', bgcolor: '#FBFAFF', borderRight: '1px solid #E4E2EF' },
        }}
      >
        <Toolbar />
        <SideMenu onNavigate={isDesktop ? undefined : () => setMobileOpen(false)} />
      </Drawer>

      <Box component="main" sx={{ flex: 1, minWidth: 0, display: 'flex', flexDirection: 'column' }}>
        <Toolbar />
        <Box sx={{ p: { xs: 1.5, sm: 2, md: 3 }, flex: 1, minWidth: 0 }}>
          <Outlet />
        </Box>
      </Box>
    </Box>
  );
}
