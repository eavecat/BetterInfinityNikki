# XAML 界面开发规范

## WPF-UI 常用控件

### Button
```xml
<ui:Button Content="点击我" 
           Appearance="Primary"
           Command="{Binding ExecuteCommand}" />

<!-- 带图标的按钮 -->
<ui:Button Icon="Play16" Content="开始" Command="{Binding StartCommand}" />
```

### TextBox
```xml
<ui:TextBox Text="{Binding InputText, UpdateSourceTrigger=PropertyChanged}"
            PlaceholderText="请输入..."
            ClearButtonEnabled="True" />
```

### CheckBox
```xml
<ui:CheckBox Content="启用功能" IsChecked="{Binding IsEnabled}" />
```

### Card
```xml
<ui:Card Margin="8">
    <StackPanel>
        <TextBlock Text="标题" FontSize="16" FontWeight="Bold" />
        <TextBlock Text="描述内容" TextWrapping="Wrap" />
    </StackPanel>
</ui:Card>
```

---

## 布局规范

### Grid 布局
```xml
<Grid>
    <Grid.RowDefinitions>
        <RowDefinition Height="Auto" />
        <RowDefinition Height="*" />
        <RowDefinition Height="Auto" />
    </Grid.RowDefinitions>
    
    <!-- 顶部 -->
    <TextBlock Grid.Row="0" Text="标题" />
    
    <!-- 中间内容 -->
    <ScrollViewer Grid.Row="1">
        <!-- 内容 -->
    </ScrollViewer>
    
    <!-- 底部按钮 -->
    <StackPanel Grid.Row="2" Orientation="Horizontal" HorizontalAlignment="Right">
        <ui:Button Content="取消" Margin="4" />
        <ui:Button Content="确定" Margin="4" Appearance="Primary" />
    </StackPanel>
</Grid>
```

### StackPanel 布局
```xml
<!-- 垂直排列 -->
<StackPanel Orientation="Vertical" Margin="16">
    <TextBlock Text="第一项" Margin="0,4" />
    <TextBlock Text="第二项" Margin="0,4" />
</StackPanel>

<!-- 水平排列 -->
<StackPanel Orientation="Horizontal">
    <ui:Button Content="按钮1" Margin="4" />
    <ui:Button Content="按钮2" Margin="4" />
</StackPanel>
```

---

## 数据绑定规范

### 基本绑定
```xml
<!-- 单向绑定 -->
<TextBlock Text="{Binding Title}" />

<!-- 双向绑定 -->
<TextBox Text="{Binding InputText, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}" />

<!-- 使用转换器 -->
<StackPanel Visibility="{Binding IsVisible, Converter={StaticResource BooleanToVisibilityConverter}}" />
<Button IsEnabled="{Binding IsLoading, Converter={StaticResource InverseBoolConverter}}" />
```

### 命令绑定
```xml
<!-- 基本命令 -->
<Button Content="执行" Command="{Binding ExecuteCommand}" />

<!-- 带参数的命令 -->
<Button Content="删除" 
        Command="{Binding DeleteCommand}"
        CommandParameter="{Binding SelectedItem}" />
```

### 事件转命令
```xml
xmlns:i="http://schemas.microsoft.com/xaml/behaviors"

<i:Interaction.Triggers>
    <i:EventTrigger EventName="Loaded">
        <i:InvokeCommandAction Command="{Binding LoadedCommand}" />
    </i:EventTrigger>
</i:Interaction.Triggers>
```

---

## 列表和集合

### ListBox
```xml
<ListBox ItemsSource="{Binding Items}" SelectedItem="{Binding SelectedItem}">
    <ListBox.ItemTemplate>
        <DataTemplate>
            <StackPanel Margin="8">
                <TextBlock Text="{Binding Name}" FontWeight="Bold" />
                <TextBlock Text="{Binding Description}" TextWrapping="Wrap" />
            </StackPanel>
        </DataTemplate>
    </ListBox.ItemTemplate>
</ListBox>
```

### DataGrid
```xml
<DataGrid ItemsSource="{Binding Items}"
          AutoGenerateColumns="False"
          SelectedItem="{Binding SelectedItem}">
    <DataGrid.Columns>
        <DataGridTextColumn Header="名称" Binding="{Binding Name}" />
        <DataGridTextColumn Header="值" Binding="{Binding Value}" />
        <DataGridCheckBoxColumn Header="启用" Binding="{Binding IsEnabled}" />
    </DataGrid.Columns>
</DataGrid>
```

---

## 性能优化

### 虚拟化
```xml
<!-- ListBox 虚拟化 -->
<ListBox ItemsSource="{Binding LargeCollection}"
         VirtualizingStackPanel.IsVirtualizing="True"
         VirtualizingStackPanel.VirtualizationMode="Recycling">
</ListBox>
```

### 延迟加载
```xml
<!-- 图片延迟加载 -->
<Image Source="{Binding ImagePath, IsAsync=True}" />
```

---

## 禁止事项

### ❌ 不要在 XAML 中编写复杂逻辑
```xml
<!-- ❌ 错误 -->
<TextBlock Text="{Binding Data, Converter={StaticResource ComplexLogicConverter}}" />

<!-- ✅ 正确：在 ViewModel 中处理逻辑 -->
<TextBlock Text="{Binding FormattedData}" />
```

### ❌ 不要过度嵌套布局
```xml
<!-- ❌ 错误：过度嵌套 -->
<Grid>
    <StackPanel>
        <Grid>
            <StackPanel>
                <!-- ... -->
            </StackPanel>
        </Grid>
    </StackPanel>
</Grid>

<!-- ✅ 正确：扁平化布局 -->
<Grid>
    <Grid.RowDefinitions>
        <RowDefinition Height="Auto" />
        <RowDefinition Height="*" />
    </Grid.RowDefinitions>
</Grid>
```

---

## 代码审查清单

- [ ] 使用了 WPF-UI 控件而非原生控件
- [ ] 所有绑定路径正确且存在对应的 ViewModel 属性
- [ ] 使用了 Behaviors 处理事件（符合 MVVM）
- [ ] 没有硬编码的魔法数字或字符串
- [ ] 布局合理，没有过度嵌套
- [ ] 考虑了性能优化（虚拟化、延迟加载等）
