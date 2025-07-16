"use client"

import { useState } from "react"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table"
import { Badge } from "@/components/ui/badge"
import { Calendar, Plus, X, BarChart3 } from "lucide-react"
import { NumberDisplay } from "@/components/ui/number-display"

interface LicenseRecord {
  id: number
  type: string
  referenceNumber: string
  description: string
  dateAdded: string
  isActive: boolean
}

export default function ArabicLicenseApp() {
  const [selectedType, setSelectedType] = useState("")
  const [referenceNumber, setReferenceNumber] = useState("")
  const [description, setDescription] = useState("")
  const [records, setRecords] = useState<LicenseRecord[]>([
    {
      id: 1,
      type: "تصريح",
      referenceNumber: "REF001",
      description: "تصريح عمل مؤقت",
      dateAdded: "2024/01/15",
      isActive: true,
    },
    {
      id: 2,
      type: "الرخص",
      referenceNumber: "LIC002",
      description: "رخصة تجارية",
      dateAdded: "2024/01/20",
      isActive: true,
    },
    {
      id: 3,
      type: "قرارات التحويل",
      referenceNumber: "TRF003",
      description: "تحويل ملكية",
      dateAdded: "2024/01/25",
      isActive: false,
    },
  ])

  const currentDate = new Date()
    .toLocaleDateString("fr-FR", {
      year: "numeric",
      month: "2-digit",
      day: "2-digit",
      hour: "2-digit",
      minute: "2-digit",
      second: "2-digit",
    })
    .replace(/(\d{2})\/(\d{2})\/(\d{4}), (\d{2}):(\d{2}):(\d{2})/, "$3/$2/$1 $4:$5:$6")

  const licenseTypes = ["تصريح", "الرخص", "قرارات التحويل", "قرارات الالغاء"]

  // إحصائيات وهمية
  const quarterlyStats = {
    تصريح: 15,
    الرخص: 8,
    "قرارات التحويل": 12,
    "قرارات الالغاء": 5,
  }

  const yearlyStats = {
    تصريح: 45,
    الرخص: 28,
    "قرارات التحويل": 35,
    "قرارات الالغاء": 18,
  }

  const handleAdd = () => {
    if (!selectedType || !referenceNumber) return

    const newRecord: LicenseRecord = {
      id: records.length + 1,
      type: selectedType,
      referenceNumber,
      description,
      dateAdded: new Date().toLocaleDateString("fr-FR").split("/").reverse().join("/"),
      isActive: true,
    }

    setRecords([...records, newRecord])
    setSelectedType("")
    setReferenceNumber("")
    setDescription("")
  }

  const handleCancel = (id: number) => {
    setRecords(records.map((record) => (record.id === id ? { ...record, isActive: false } : record)))
  }

  return (
    <div className="min-h-screen bg-gray-50 p-6" dir="rtl" style={{ fontVariantNumeric: "lining-nums" }}>
      <div className="max-w-7xl mx-auto space-y-6">
        {/* Header with Current Date */}
        <div className="bg-white rounded-lg shadow-sm p-4 border-r-4 border-blue-500">
          <div className="flex items-center gap-3">
            <Calendar className="h-6 w-6 text-blue-600" />
            <h1 className="text-2xl font-bold text-gray-800">تطبيق إدارة الرخص</h1>
            <div className="mr-auto">
              <Badge variant="outline" className="text-lg px-4 py-2">
                التاريخ الحالي: {currentDate}
              </Badge>
            </div>
          </div>
        </div>

        <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
          {/* Add License Form */}
          <Card className="shadow-lg">
            <CardHeader className="bg-green-50 border-b">
              <CardTitle className="flex items-center gap-2 text-green-800">
                <Plus className="h-5 w-5" />
                إضافة رخصة جديدة
              </CardTitle>
            </CardHeader>
            <CardContent className="p-6 space-y-4">
              <div className="space-y-2">
                <Label htmlFor="type" className="text-right block font-semibold">
                  نوع الرخصة
                </Label>
                <Select value={selectedType} onValueChange={setSelectedType}>
                  <SelectTrigger className="text-right">
                    <SelectValue placeholder="اختر نوع الرخصة" />
                  </SelectTrigger>
                  <SelectContent>
                    {licenseTypes.map((type) => (
                      <SelectItem key={type} value={type} className="text-right">
                        {type}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>

              <div className="space-y-2">
                <Label htmlFor="reference" className="text-right block font-semibold">
                  رقم المرجع
                </Label>
                <Input
                  id="reference"
                  value={referenceNumber}
                  onChange={(e) => setReferenceNumber(e.target.value)}
                  placeholder="أدخل رقم المرجع"
                  className="text-right"
                />
              </div>

              <div className="space-y-2">
                <Label htmlFor="description" className="text-right block font-semibold">
                  الوصف
                </Label>
                <Input
                  id="description"
                  value={description}
                  onChange={(e) => setDescription(e.target.value)}
                  placeholder="أدخل وصف الرخصة"
                  className="text-right"
                />
              </div>

              <div className="flex gap-3 pt-4">
                <Button
                  onClick={handleAdd}
                  className="bg-green-600 hover:bg-green-700 flex-1"
                  disabled={!selectedType || !referenceNumber}
                >
                  <Plus className="h-4 w-4 ml-2" />
                  إضافة
                </Button>
              </div>
            </CardContent>
          </Card>

          {/* Statistics */}
          <Card className="shadow-lg">
            <CardHeader className="bg-blue-50 border-b">
              <CardTitle className="flex items-center gap-2 text-blue-800">
                <BarChart3 className="h-5 w-5" />
                الإحصائيات
              </CardTitle>
            </CardHeader>
            <CardContent className="p-6">
              <div className="grid grid-cols-2 gap-6">
                {/* Quarterly Stats */}
                <div className="space-y-3">
                  <h3 className="font-bold text-gray-700 border-b pb-2">إحصائيات الربع الحالي (7-8-9)</h3>
                  {Object.entries(quarterlyStats).map(([type, count]) => (
                    <div key={type} className="flex justify-between items-center p-2 bg-gray-50 rounded">
                      <span className="font-medium">{type}</span>
                      <Badge variant="secondary" className="bg-blue-100 text-blue-800">
                        <NumberDisplay value={count} />
                      </Badge>
                    </div>
                  ))}
                </div>

                {/* Yearly Stats */}
                <div className="space-y-3">
                  <h3 className="font-bold text-gray-700 border-b pb-2">إحصائيات السنة الحالية</h3>
                  {Object.entries(yearlyStats).map(([type, count]) => (
                    <div key={type} className="flex justify-between items-center p-2 bg-gray-50 rounded">
                      <span className="font-medium">{type}</span>
                      <Badge variant="secondary" className="bg-green-100 text-green-800">
                        <NumberDisplay value={count} />
                      </Badge>
                    </div>
                  ))}
                </div>
              </div>
            </CardContent>
          </Card>
        </div>

        {/* Records Table */}
        <Card className="shadow-lg">
          <CardHeader className="bg-gray-50 border-b">
            <CardTitle className="text-gray-800">سجل الرخص والتصاريح</CardTitle>
          </CardHeader>
          <CardContent className="p-0">
            <Table>
              <TableHeader>
                <TableRow className="bg-gray-100">
                  <TableHead className="text-right font-bold">الرقم</TableHead>
                  <TableHead className="text-right font-bold">النوع</TableHead>
                  <TableHead className="text-right font-bold">رقم المرجع</TableHead>
                  <TableHead className="text-right font-bold">الوصف</TableHead>
                  <TableHead className="text-right font-bold">تاريخ الإضافة</TableHead>
                  <TableHead className="text-right font-bold">الحالة</TableHead>
                  <TableHead className="text-right font-bold">الإجراءات</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {records.map((record) => (
                  <TableRow key={record.id} className="hover:bg-gray-50">
                    <TableCell className="text-right font-medium">
                      <NumberDisplay value={record.id} />
                    </TableCell>
                    <TableCell className="text-right">
                      <Badge variant="outline" className="bg-blue-50 text-blue-700">
                        {record.type}
                      </Badge>
                    </TableCell>
                    <TableCell className="text-right">{record.referenceNumber}</TableCell>
                    <TableCell className="text-right">{record.description}</TableCell>
                    <TableCell className="text-right">{record.dateAdded}</TableCell>
                    <TableCell className="text-right">
                      <Badge
                        variant={record.isActive ? "default" : "destructive"}
                        className={record.isActive ? "bg-green-100 text-green-800" : "bg-red-100 text-red-800"}
                      >
                        {record.isActive ? "نشط" : "ملغى"}
                      </Badge>
                    </TableCell>
                    <TableCell className="text-right">
                      {record.isActive && (
                        <Button
                          variant="destructive"
                          size="sm"
                          onClick={() => handleCancel(record.id)}
                          className="bg-red-500 hover:bg-red-600"
                        >
                          <X className="h-4 w-4 ml-1" />
                          إلغاء
                        </Button>
                      )}
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </CardContent>
        </Card>
      </div>
    </div>
  )
}
