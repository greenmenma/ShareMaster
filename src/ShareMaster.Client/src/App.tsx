import { createBrowserRouter, RouterProvider } from 'react-router';
import { AppLayout } from './layout/AppLayout';
import { HomePage } from './pages/HomePage';
import { DummyPage } from './pages/DummyPage';
import { NotFoundPage } from './pages/NotFoundPage';
import { CalendarListPage } from './pages/calendars/CalendarListPage';
import { CalendarViewPage } from './pages/calendars/CalendarViewPage';
import { CalendarFormPage } from './pages/calendars/CalendarFormPage';
import { DayTypesPage } from './pages/calendars/DayTypesPage';
import { PublicHolidaysPage } from './pages/calendars/PublicHolidaysPage';
import { HistoryPage } from './pages/calendars/HistoryPage';
import { SystemAdminsPage } from './pages/SystemAdminsPage';

const router = createBrowserRouter([
  {
    path: '/',
    element: <AppLayout />,
    children: [
      { index: true, element: <HomePage /> },
      { path: 'calendars', element: <CalendarListPage /> },
      { path: 'calendars/new', element: <CalendarFormPage /> },
      { path: 'calendars/day-types', element: <DayTypesPage /> },
      { path: 'calendars/public-holidays', element: <PublicHolidaysPage /> },
      { path: 'calendars/history', element: <HistoryPage /> },
      { path: 'calendars/:id', element: <CalendarViewPage /> },
      { path: 'calendars/:id/settings', element: <CalendarFormPage /> },
      { path: 'accounts', element: <DummyPage title="口座" /> },
      { path: 'order-staff', element: <DummyPage title="発注担当コード" /> },
      { path: 'partners', element: <DummyPage title="取引先" /> },
      { path: 'order-types', element: <DummyPage title="受注型式" /> },
      { path: 'admin/system-admins', element: <SystemAdminsPage /> },
      { path: '*', element: <NotFoundPage /> },
    ],
  },
]);

export function App() {
  return <RouterProvider router={router} />;
}
