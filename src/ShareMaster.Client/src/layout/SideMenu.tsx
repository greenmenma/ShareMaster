import { useState } from 'react';
import { NavLink, useLocation } from 'react-router';
import {
  Box,
  Chip,
  Collapse,
  IconButton,
  List,
  ListItemButton,
  ListItemIcon,
  ListItemText,
  ListSubheader,
  Tooltip,
} from '@mui/material';
import CalendarMonthIcon from '@mui/icons-material/CalendarMonth';
import ExpandLess from '@mui/icons-material/ExpandLess';
import ExpandMore from '@mui/icons-material/ExpandMore';
import TableChartIcon from '@mui/icons-material/TableChart';
import AddIcon from '@mui/icons-material/Add';
import PaletteIcon from '@mui/icons-material/Palette';
import FlagIcon from '@mui/icons-material/Flag';
import HistoryIcon from '@mui/icons-material/History';
import AdminPanelSettingsIcon from '@mui/icons-material/AdminPanelSettings';
import HomeIcon from '@mui/icons-material/Home';
import EventIcon from '@mui/icons-material/Event';
import { useCalendars, useMe } from '../api/queries';
import { buildTree, type CalendarTreeNode } from '../utils/calendarTree';
import { dummyMenus } from './menu';

interface Props {
  onNavigate?: () => void;
}

const itemSx = {
  borderRadius: 1,
  mx: 1,
  '&.active': { bgcolor: 'primary.main', color: 'primary.contrastText', '& .MuiListItemIcon-root': { color: 'inherit' } },
};

export function SideMenu({ onNavigate }: Props) {
  const location = useLocation();
  const { data: me } = useMe();
  const { data: calendars = [] } = useCalendars();
  const [calendarOpen, setCalendarOpen] = useState(true);
  const [treeOpen, setTreeOpen] = useState(true);
  const tree = buildTree(calendars);

  const renderNode = (node: CalendarTreeNode) => (
    <Box key={node.calendar.id}>
      <ListItemButton
        component={NavLink}
        to={`/calendars/${node.calendar.id}`}
        onClick={onNavigate}
        dense
        sx={{ ...itemSx, pl: 5 + node.depth * 2 }}
      >
        <ListItemIcon sx={{ minWidth: 28 }}>
          <EventIcon fontSize="small" />
        </ListItemIcon>
        <ListItemText primary={node.calendar.name} slotProps={{ primary: { noWrap: true, sx: { fontSize: 14 } } }} />
      </ListItemButton>
      {node.children.map(renderNode)}
    </Box>
  );

  return (
    <Box component="nav" sx={{ py: 1, overflowY: 'auto', flex: 1 }}>
      <List dense disablePadding>
        <ListItemButton component={NavLink} to="/" end onClick={onNavigate} sx={itemSx}>
          <ListItemIcon><HomeIcon /></ListItemIcon>
          <ListItemText primary="ホーム" />
        </ListItemButton>

        <ListItemButton onClick={() => setCalendarOpen(!calendarOpen)} sx={{ mx: 1, borderRadius: 1 }}>
          <ListItemIcon><CalendarMonthIcon /></ListItemIcon>
          <ListItemText primary="カレンダー" />
          {calendarOpen ? <ExpandLess /> : <ExpandMore />}
        </ListItemButton>
        <Collapse in={calendarOpen} timeout="auto" unmountOnExit>
          <List dense disablePadding>
            <ListItemButton component={NavLink} to="/calendars" end onClick={onNavigate} sx={{ ...itemSx, pl: 4 }}>
              <ListItemIcon sx={{ minWidth: 36 }}><TableChartIcon fontSize="small" /></ListItemIcon>
              <ListItemText primary="一覧" />
            </ListItemButton>
            <ListItemButton
              onClick={() => setTreeOpen(!treeOpen)}
              sx={{ mx: 1, borderRadius: 1, pl: 4 }}
              selected={/^\/calendars\/\d+/.test(location.pathname)}
            >
              <ListItemIcon sx={{ minWidth: 36 }}><CalendarMonthIcon fontSize="small" /></ListItemIcon>
              <ListItemText primary="カレンダー" />
              {me?.isSystemAdmin && (
                <Tooltip title="カレンダーを追加">
                  <IconButton
                    size="small"
                    component={NavLink}
                    to="/calendars/new"
                    onClick={(e) => {
                      e.stopPropagation();
                      onNavigate?.();
                    }}
                  >
                    <AddIcon fontSize="small" />
                  </IconButton>
                </Tooltip>
              )}
              {treeOpen ? <ExpandLess fontSize="small" /> : <ExpandMore fontSize="small" />}
            </ListItemButton>
            <Collapse in={treeOpen} timeout="auto" unmountOnExit>
              {tree.map(renderNode)}
            </Collapse>
            <ListItemButton component={NavLink} to="/calendars/day-types" onClick={onNavigate} sx={{ ...itemSx, pl: 4 }}>
              <ListItemIcon sx={{ minWidth: 36 }}><PaletteIcon fontSize="small" /></ListItemIcon>
              <ListItemText primary="日種別・表示色" />
            </ListItemButton>
            <ListItemButton component={NavLink} to="/calendars/public-holidays" onClick={onNavigate} sx={{ ...itemSx, pl: 4 }}>
              <ListItemIcon sx={{ minWidth: 36 }}><FlagIcon fontSize="small" /></ListItemIcon>
              <ListItemText primary="祝日" />
            </ListItemButton>
            <ListItemButton component={NavLink} to="/calendars/history" onClick={onNavigate} sx={{ ...itemSx, pl: 4 }}>
              <ListItemIcon sx={{ minWidth: 36 }}><HistoryIcon fontSize="small" /></ListItemIcon>
              <ListItemText primary="変更履歴" />
            </ListItemButton>
          </List>
        </Collapse>

        {dummyMenus.map((m) => (
          <ListItemButton key={m.path} component={NavLink} to={m.path} onClick={onNavigate} sx={itemSx}>
            <ListItemIcon>{m.icon}</ListItemIcon>
            <ListItemText primary={m.label} />
            {m.dummy && <Chip label="準備中" size="small" variant="outlined" sx={{ height: 20, fontSize: 11 }} />}
          </ListItemButton>
        ))}
      </List>

      {me?.isSystemAdmin && (
        <List dense subheader={<ListSubheader sx={{ bgcolor: 'transparent', lineHeight: '32px' }}>管理</ListSubheader>}>
          <ListItemButton component={NavLink} to="/admin/system-admins" onClick={onNavigate} sx={itemSx}>
            <ListItemIcon><AdminPanelSettingsIcon /></ListItemIcon>
            <ListItemText primary="システム管理者" />
          </ListItemButton>
        </List>
      )}
    </Box>
  );
}
